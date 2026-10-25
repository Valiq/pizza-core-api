using Dapper;
using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class PizzaService : IPizzaService
    {
        protected readonly IConfiguration Configuration;
        private readonly ISessionService SessionService;

        public PizzaService(IConfiguration configuration, ISessionService sessionService)
        {
            Configuration = configuration;
            SessionService = sessionService;
        }

        public async Task<Pizza> GetPizza(long id, string? token)
        {
            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                bool needHideDeleted = true;

                if (token is not null)
                {
                    UserRole[] roles = { UserRole.Admin, UserRole.Customer };
                    var parsed = await SessionService.CheckGrants(token, roles);
                    needHideDeleted = (parsed.Value.userRole != UserRole.Admin.Code);
                }

                return await GetPizzaSql(id, connection, needHideDeleted);
            }
        }

        public async Task<Pizza> GetPizzaSql(long id, MySqlConnection connection, bool needHideDeleted)
        {
            var sql = @"
            SELECT 
                p.id, 
                p.type, 
                p.size, 
                p.name, 
                p.base_price as basePrice, 
                p.image_path as imagePath, 
                p.is_deleted as isDeleted, 
                DATE_FORMAT(p.created_at, ''%Y-%m-%dT%H:%i:%s'') as createdAt, 
                DATE_FORMAT(p.updated_at, ''%Y-%m-%dT%H:%i:%s'') as updatedAt, 
                updater.id, 
                updater.name, 
                updater.role,
                i.id, 
                i.name, 
                i.type
            FROM pizza p
            LEFT JOIN user updater ON p.updated_by = updater.id
            LEFT JOIN pizza_ingredient pi ON p.id = pi.pizza_id
            LEFT JOIN ingredient i ON pi.ingredient_id = i.id AND i.is_deleted = false
            WHERE p.id = @id";

            if (needHideDeleted)
                sql = $"{sql} AND p.is_deleted = false"; 

            var pizza = (await connection.QueryAsync<Pizza, UserShort, List <IngredientShort>, Pizza>(
                sql,
                (pizza, updater, ingredients) =>
                {
                    return new Pizza
                    {
                        id = pizza.id,
                        type = pizza.type,
                        size = pizza.size,
                        name = pizza.name,
                        basePrice = pizza.basePrice,
                        imagePath = pizza.imagePath,
                        isDeleted = pizza.isDeleted,
                        createdAt = pizza.createdAt,
                        updatedAt = pizza.updatedAt,
                        updatedBy = updater,
                        ingredients = ingredients
                    };
                },
                new { id },
                splitOn: "id"
            )).FirstOrDefault();

            if (pizza is null)
                throw new KeyNotFoundException($"Пицца {id} не найдена");

            return pizza;
        }

        public async Task<PizzasResponse> GetPizzasPaged(PizzaFilterRequest request, string? token)
        {
            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                int offset = (request.pageNumber - 1) * request.pageSize;

                var whereConditions = new List<string>();
                var parameters = new DynamicParameters();

                bool isAdmin = false;

                if (token is not null)
                {
                    UserRole[] roles = { UserRole.Admin, UserRole.Customer };
                    var parsed = await SessionService.CheckGrants(token, roles);
                    isAdmin = (parsed.Value.userRole != UserRole.Admin.Code);
                }

                whereConditions.Add("p.is_deleted = @IsDeleted");

                if (request is not null)
                {
                    if (isAdmin)
                        parameters.Add("IsDeleted", request.isDeleted ?? true);
                    else
                        parameters.Add("IsDeleted", false);
                }
                else if (!isAdmin)
                    parameters.Add("IsDeleted", false);

                if (!string.IsNullOrEmpty(request.name))
                {
                    whereConditions.Add("p.name LIKE @Name");
                    parameters.Add("Name", $"%{request.name}%");
                }

                if (!string.IsNullOrEmpty(request.type))
                {
                    whereConditions.Add("p.type LIKE @Type");
                    parameters.Add("Type", $"%{request.type}%");
                }

                if (!string.IsNullOrEmpty(request.size))
                {
                    whereConditions.Add("p.size LIKE @Size");
                    parameters.Add("Size", $"%{request.size}%");
                }

                if (!string.IsNullOrEmpty(request.createdAtStart))
                {
                    whereConditions.Add("p.created_at >= @CreatedAtStart");
                    parameters.Add("CreatedAtStart", request.createdAtStart);
                }

                if (!string.IsNullOrEmpty(request.createdAtEnd))
                {
                    whereConditions.Add("p.created_at <= @CreatedAtEnd");
                    parameters.Add("CreatedAtEnd", request.createdAtEnd);
                }

                if (request.ingredients is not null && request.ingredients.Length > 0)
                {
                    whereConditions.Add("p.id IN (SELECT pizza_id FROM pizza_ingredient WHERE ingredient_id IN @IngredientIds)");
                    parameters.Add("IngredientIds", request.ingredients);
                }

                string whereClause = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
                    : "";

                var validSortColumns = new Dictionary<string, string>
                {
                    ["id"] = "p.id",
                    ["name"] = "p.name",
                    ["type"] = "p.type",
                    ["size"] = "p.size",
                    ["baseprice"] = "p.base_price",
                    ["createdat"] = "p.created_at",
                    ["updatedat"] = "p.updated_at"
                };

                var sortColumnKey = request.sortBy?.ToLower();
                string sortColumn = "p.created_at";

                if (sortColumnKey is not null && validSortColumns.ContainsKey(sortColumnKey))
                    sortColumn = validSortColumns[sortColumnKey];

                string orderByClause = $"ORDER BY {sortColumn} {request.sortType ?? "DESC"}";

                var countSql = $@"
                    SELECT COUNT(*)
                    FROM pizza p
                    {whereClause}";

                int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                int totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);

                var sql = $@"
                    SELECT
                        p.id,
                        p.type,
                        p.size,
                        p.name,
                        p.base_price as basePrice,
                        p.image_path as imagePath,
                        p.is_deleted as isDeleted,
                        DATE_FORMAT(p.created_at, ''%Y-%m-%dT%H:%i:%s'') as createdAt,
                        DATE_FORMAT(p.updated_at, ''%Y-%m-%dT%H:%i:%s'') as updatedAt,
                        p.updated_by,
                        updater.id,
                        updater.name,
                        updater.role,
                        i.id as ing_id,
                        i.name as ing_name,
                        i.type as ing_type,
                        i.base_price as ing_basePrice,
                        i.is_deleted as ing_isDeleted,
                        DATE_FORMAT(i.created_at, ''%Y-%m-%dT%H:%i:%s'') as ing_createdAt,
                        DATE_FORMAT(i.updated_at, ''%Y-%m-%dT%H:%i:%s'') as ing_updatedAt,
                        iu.id as ing_updater_id,
                        iu.name as ing_updater_name,
                        iu.role as ing_updater_type
                    FROM pizza p
                    LEFT JOIN user updater ON p.updated_by = updater.id
                    LEFT JOIN pizza_ingredient pi ON p.id = pi.pizza_id
                    LEFT JOIN ingredient i ON pi.ingredient_id = i.id AND i.is_deleted = false
                    LEFT JOIN user iu ON i.updated_by = iu.id
                    {whereClause}
                    {orderByClause}
                    LIMIT @PageSize OFFSET @Offset";

                parameters.Add("PageSize", request.pageSize);
                parameters.Add("Offset", offset);

                var pizzaCache = new Dictionary<long, Pizza>();

                var rows = await connection.QueryAsync(sql, parameters);

                foreach (var row in rows)
                {
                    long pizzaId = row.id;

                    if (!pizzaCache.TryGetValue(pizzaId, out var pizza))
                    {
                        pizza = new Pizza
                        {
                            id = pizzaId,
                            type = row.type,
                            size = row.size,
                            name = row.name,
                            basePrice = row.basePrice,
                            imagePath = row.imagePath,
                            isDeleted = row.isDeleted,
                            createdAt = row.createdAt,
                            updatedAt = row.updatedAt,
                            updatedBy = row.updater_id is not null
                                ? new UserShort { id = row.updater_id, name = row.updater_name, type = row.updater_role }
                                : null,
                            ingredients = new List<IngredientShort>()
                        };
                        pizzaCache[pizzaId] = pizza;
                    }

                    if (row.ing_id is not null)
                    {
                        pizza.ingredients.Add(new IngredientShort
                        {
                            id = row.ing_id,
                            name = row.ing_name,
                            type = row.ing_type
                        });
                    }
                }

                var pizzas = pizzaCache.Values.ToList();

                return new PizzasResponse(
                    pizzas,
                    totalCount,
                    request.pageNumber,
                    request.pageSize,
                    totalPages
                );
            }
        }

        public async Task<Pizza> CreatePizza(PizzaCreateRequest request, string? token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var sql = @"
                        INSERT INTO pizza (name, type, size, base_price, image_path, updated_by)
                        VALUES (@name, @type, @size, @basePrice, @imagePath, @updatedBy);
                        SELECT LAST_INSERT_ID();";

                var parameters = new
                {
                    request.name,
                    request.type,
                    request.size,
                    request.basePrice,
                    request.imagePath,
                    updatedBy = parsed.Value.userId
                };

                var newId = await connection.ExecuteScalarAsync<long>(sql, parameters);

                // Вставляем ингредиенты пиццы
                if (request.ingredients is not null && request.ingredients.Length > 0)
                {
                    var ingredientSql = @"
                        INSERT INTO pizza_ingredient (pizza_id, ingredient_id, quantity)
                        VALUES (@PizzaId, @IngredientId, 1.000)";

                    foreach (var ingredientId in request.ingredients)
                    {
                        await connection.ExecuteAsync(ingredientSql, new
                        {
                            PizzaId = newId,
                            IngredientId = ingredientId
                        });
                    }
                }

                return await GetPizzaSql(newId, connection, true);
            }
        }

        public async Task<Pizza> UpdatePizza(long id, PizzaUpdateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                // Проверка существования пиццы
                var pizzaExistsSql = "SELECT COUNT(*) FROM pizza WHERE id = @id AND is_deleted = false";
                var pizzaExists = await connection.ExecuteScalarAsync<int>(pizzaExistsSql, new { id });

                if (pizzaExists == 0)
                    throw new KeyNotFoundException($"Пицца не найдена по заданному Id = {id}");

                var parameters = new
                {
                    request.name,
                    request.type,
                    request.size,
                    request.basePrice,
                    request.imagePath,
                    updatedBy = parsed.Value.userId,
                    id
                };

                var sql = $@"
                    UPDATE pizza
                    SET name = @name, type = @type, size = @size, base_price = @basePrice, image_path = @imagePath, updated_by = @updatedBy
                    WHERE id = @id";

                await connection.ExecuteAsync(sql, parameters);

                // Обновляем ингредиенты пиццы (полное заменение связей)
                if (request.ingredients is not null)
                {
                    // Удаляем старые связи
                    var deleteSql = "DELETE FROM pizza_ingredient WHERE pizza_id = @PizzaId";
                    await connection.ExecuteAsync(deleteSql, new { PizzaId = id });

                    // Вставляем новые связи
                    if (request.ingredients.Length > 0)
                    {
                        var ingredientSql = @"
                            INSERT INTO pizza_ingredient (pizza_id, ingredient_id, quantity)
                            VALUES (@PizzaId, @IngredientId, 1.000)";

                        foreach (var ingredientId in request.ingredients)
                        {
                            await connection.ExecuteAsync(ingredientSql, new
                            {
                                PizzaId = id,
                                IngredientId = ingredientId
                            });
                        }
                    }
                }

                return await GetPizzaSql(id, connection, false);
            }
        }

        public async Task<bool> DeletePizza(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin };

            await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                connection.Open();

                var pizzaExistsSql = "SELECT COUNT(*) FROM pizza WHERE id = @id AND is_deleted = false";
                var pizzaExists = connection.ExecuteScalar<int>(pizzaExistsSql, new { id });

                if (pizzaExists == 0)
                    throw new KeyNotFoundException($"Пицца {id} не найдена или уже удалена");

                var deleteSql = @"
                    UPDATE pizza 
                    SET 
                        is_deleted = true,
                        updated_at = NOW()
                    WHERE id = @id";

                int affectedRows = await connection.ExecuteAsync(deleteSql, new { id });

                return (affectedRows > 0);
            }
        }
    }
}