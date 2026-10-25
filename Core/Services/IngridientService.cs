using Dapper;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using System.Data;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class IngridientService : IIngredientService
    {
        protected readonly IConfiguration Configuration;
        private readonly ISessionService SessionService;

        public IngridientService(IConfiguration configuration, ISessionService sessionService)
        {
            Configuration = configuration;
            SessionService = sessionService;
        }

        public async Task<Ingredient> GetIngredient(long id, string? token)
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

                return await IngredientSql(id, connection, needHideDeleted);
            }
        }

        public async Task<Ingredient> IngredientSql(long id, MySqlConnection connection, bool needHideDeleted)
        {
            var sql = @"
                    SELECT 
                        i.id,
                        i.name,
                        i.type,
                        i.base_price as basePrice,
                        i.is_deleted as isDeleted,
                        DATE_FORMAT(i.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(i.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt,
                        u.id,
                        u.name,
                        u.role as type
                    FROM ingredient i
                    LEFT JOIN user u ON i.updated_by = u.id
                    WHERE i.id = @id";

            if (needHideDeleted)
                sql = $"{sql} AND i.is_deleted = false";

            var ingredient = (await connection.QueryAsync<Ingredient, UserShort, Ingredient>(
                sql,
                (ingredient, updatedBy) =>
                {
                    return new Ingredient
                    {
                        id = ingredient.id,
                        name = ingredient.name,
                        type = ingredient.type,
                        basePrice = ingredient.basePrice,
                        isDeleted = ingredient.isDeleted,
                        createdAt = ingredient.createdAt,
                        updatedAt = ingredient.updatedAt,
                        updatedBy = updatedBy
                    };
                },
                new { id },
                splitOn: "id"
            )).FirstOrDefault();

            if (ingredient is null)
                throw new KeyNotFoundException($"Ингредиент с идентификатором {id} не найден или удалён");

            return ingredient;
        }

        public async Task<IngredientsResponse> GetIngredientsPaged(IngredientFilterRequest request, string? token)
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

                whereConditions.Add("i.is_deleted = @IsDeleted");

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
                    whereConditions.Add("i.name LIKE @Name");
                    parameters.Add("Name", $"%{request.name}%");
                }

                if (!string.IsNullOrEmpty(request.type))
                {
                    whereConditions.Add("i.type LIKE @Type");
                    parameters.Add("Type", $"%{request.type}%");
                }

                if (!string.IsNullOrEmpty(request.createdAtStart))
                {
                    whereConditions.Add("i.created_at >= @CreatedAtStart");
                    parameters.Add("CreatedAtStart", request.createdAtStart);
                }

                if (!string.IsNullOrEmpty(request.createdAtEnd))
                {
                    whereConditions.Add("i.created_at <= @CreatedAtEnd");
                    parameters.Add("CreatedAtEnd", request.createdAtEnd);
                }

                string whereClause = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
                    : "";

                var validSortColumns = new Dictionary<string, string>
                {
                    ["id"] = "i.id",
                    ["name"] = "i.name",
                    ["type"] = "i.type",
                    ["baseprice"] = "i.base_price",
                    ["createdat"] = "i.created_at",
                    ["updatedat"] = "i.updated_at"
                };

                var sortColumnKey = request.sortBy?.ToLower();
                string sortColumn = "i.created_at";

                if (sortColumnKey is not null && validSortColumns.ContainsKey(sortColumnKey))
                    sortColumn = validSortColumns[sortColumnKey];

                string orderByClause = $"ORDER BY {sortColumn} {request.sortType ?? "DESC"}";

                var countSql = $@"
                    SELECT COUNT(*)
                    FROM ingredient i
                    {whereClause}";

                int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                int totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);

                var sql = $@"
                    SELECT
                        i.id,
                        i.name,
                        i.type,
                        i.base_price as basePrice,
                        i.is_deleted as isDeleted,
                        DATE_FORMAT(i.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(i.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt,
                        u.id,
                        u.name,
                        u.role as type
                    FROM ingredient i
                    LEFT JOIN user u ON i.updated_by = u.id
                    {whereClause}
                    {orderByClause}
                    LIMIT @PageSize OFFSET @Offset";

                parameters.Add("PageSize", request.pageSize);
                parameters.Add("Offset", offset);

                var ingredients = (await connection.QueryAsync<Ingredient, UserShort, Ingredient>(
                    sql,
                    (ingredient, updatedBy) =>
                    {
                        ingredient.updatedBy = updatedBy;
                        return ingredient;
                    },
                    parameters,
                    splitOn: "id"
                )).ToList();

                return new IngredientsResponse(
                    ingredients,
                    totalCount,
                    request.pageNumber,
                    request.pageSize,
                    totalPages
                );
            }
        }

        public async Task<Ingredient> UpdateIngredient(long id, IngredientUpdateRequest request, string? token)
        {
            UserRole[] roles = { UserRole.Admin };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var ingredientExistsSql = "SELECT COUNT(*) FROM ingredient WHERE id = @id AND is_deleted = false";
                var ingredientExists = await connection.ExecuteScalarAsync<int>(ingredientExistsSql, new { id });

                if (ingredientExists == 0)
                    throw new KeyNotFoundException($"Ингредиент не найден по заданному Id = {id}");

                var parameters = new
                {
                    Id = id,
                    Name = request.name,
                    Type = request.type,
                    BasePrice = request.basePrice,
                    UpdatedBy = parsed.Value.userId
                };

                var sql = @"
                    UPDATE ingredient
                    SET name = @Name,
                        type = @Type,
                        base_price = @BasePrice,
                        updated_by = @UpdatedBy
                    WHERE id = @Id";

                await connection.ExecuteAsync(sql, parameters);

                return await IngredientSql(id, connection, false);
            }
        }

        public async Task<Ingredient> CreateIngredient(IngredientCreateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ingredient (name, type, base_price, updated_by)
                    VALUES (@Name, @Type, @BasePrice, @UpdatedBy);
                    SELECT LAST_INSERT_ID();";

                var parameters = new
                {
                    Name = request.name,
                    Type = request.type,
                    BasePrice = request.basePrice,
                    UpdatedBy = parsed.Value.userId
                };

                var newIngredientId = await connection.ExecuteScalarAsync<long>(sql, parameters);

                return await IngredientSql(newIngredientId, connection, false); ;
            }
        }

        public async Task<bool> DeleteIngredient(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var ingredientExistsSql = "SELECT COUNT(*) FROM ingredient WHERE id = @id AND is_deleted = false";
                var ingredientExists = await connection.ExecuteScalarAsync<int>(ingredientExistsSql, new { id });

                if (ingredientExists == 0)
                    throw new KeyNotFoundException("Ингредиент не найден или уже удален");

                var deleteSql = @"
                    UPDATE ingredient
                    SET is_deleted = true
                    WHERE id = @id";

                int affectedRows = await connection.ExecuteAsync(deleteSql, new { id });

                return (affectedRows > 0);
            }
        }
    }
}
