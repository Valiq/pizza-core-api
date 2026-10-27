using Dapper;
using Microsoft.AspNetCore.Authorization;

using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using System.Data;
using static BCrypt.Net.BCrypt;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class UserService : IUserService
    {
        protected readonly IConfiguration Configuration;
        private readonly ISessionService SessionService;

        public UserService(IConfiguration configuration, ISessionService sessionService)
        {
            Configuration = configuration;
            SessionService = sessionService;
        }

        public async Task<UserResponse> GetUser(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            if (parsed.Value.userId != id && parsed.Value.userRole != UserRole.Admin.Code)
                throw new InvalidOperationException($"Данный идентификатор пользователя - {id} не пренадлежит вам");

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                bool needHideDeleted = (parsed.Value.userRole != UserRole.Admin.Code);

                return await GetUserSql(id, connection, needHideDeleted);
            }
        }
         
        public async Task<UserResponse> GetUserSql(long id, MySqlConnection connection, bool needHideDeleted)
        {
            // Запрос 1: получаем пользователя по id (вместе с тем, кто обновлял запись)
            var userSql = @"
                    SELECT
                        u.id,
                        u.name,
                        u.email,
                        u.phone,
                        u.is_deleted as isDeleted,
                        DATE_FORMAT(u.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(u.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt,
                        creator.id,
                        creator.name,
                        creator.role as type
                    FROM user u
                    LEFT JOIN user creator ON u.updated_by = creator.id
                    WHERE u.id = @id";

            if (needHideDeleted)
                userSql = $"{userSql} AND u.is_deleted = false";

            var user = (await connection.QueryAsync<User, UserShort, User>(
                userSql,
                (user, creator) =>
                {
                    user.updatedBy = creator;
                    return user;
                },
                new { id },
                splitOn: "id"
            )).FirstOrDefault();

            if (user is null)
                throw new KeyNotFoundException($"Пользователь {id} не найден или удалён");

            // Запрос 2: получаем адреса пользователя
            var adressSql = @"
                    SELECT
                        a.id,
                        a.city,
                        a.street,
                        a.house,
                        a.apartment,
                        DATE_FORMAT(a.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(a.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt
                    FROM adress a
                    WHERE a.user_id = @id";

            var adresses = (await connection.QueryAsync<Adress>(adressSql, new { id })).ToList();

            return new UserResponse(
                id: user.id,
                name: user.name,
                email: user.email,
                phone: user.phone,
                role: user.role,
                isDeleted: user.isDeleted,
                createdAt: user.createdAt,
                updatedAt: user.updatedAt,
                adresses: adresses,
                updatedBy: user.updatedBy
            );
        }

        public async Task<UsersResponse> GetUsersPaged(UserFilterRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin };

            var parsed = await SessionService.CheckGrants(token, roles);
            if (parsed is null)
                throw new UnauthorizedAccessException("Недостаточно прав для просмотра списка пользователей");

            int pageNumber = request.pageNumber < 1 ? 1 : request.pageNumber;
            int pageSize = request.pageSize < 1 ? 10 : request.pageSize;

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                int offset = (request.pageNumber - 1) * request.pageSize;

                // Динамическое построение WHERE условия
                var whereConditions = new List<string>();
                var parameters = new DynamicParameters();

                // Базовое условие (исключаем мягко удалённых по умолчанию)
                whereConditions.Add("u.is_deleted = @IsDeleted");
                parameters.Add("IsDeleted", request.isDeleted ?? false);

                // Фильтрация по email
                if (!string.IsNullOrEmpty(request.email))
                {
                    whereConditions.Add("u.email LIKE @Email");
                    parameters.Add("Email", $"%{request.email}%");
                }

                // Фильтрация по name
                if (!string.IsNullOrEmpty(request.name))
                {
                    whereConditions.Add("u.name LIKE @Name");
                    parameters.Add("Name", $"%{request.name}%");
                }

                // Фильтрация по phone
                if (!string.IsNullOrEmpty(request.phone))
                {
                    whereConditions.Add("u.phone LIKE @Phone");
                    parameters.Add("Phone", $"%{request.phone}%");
                }

                // Фильтрация по адресу (поиск по LIKE в полях city, street, house, apartment)
                if (!string.IsNullOrEmpty(request.adress))
                {
                    whereConditions.Add("(a.city LIKE @Adress OR a.street LIKE @Adress OR a.house LIKE @Adress OR a.apartment LIKE @Adress)");
                    parameters.Add("Adress", $"%{request.adress}%");
                }

                // Фильтрация по ролям (роль хранится строкой в user.role)
                if (request.role is not null && request.role.Length > 0)
                {
                    whereConditions.Add("u.role IN @Roles");
                    parameters.Add("Roles", request.role);
                }

                // Фильтрация по создателю (кто обновил запись, по имени пользователя)
                if (!string.IsNullOrEmpty(request.createdBy))
                {
                    whereConditions.Add("creator.name LIKE @CreateByName");
                    parameters.Add("CreateByName", $"%{request.createdBy}%");
                }

                // Фильтрация по дате создания (начало)
                if (!string.IsNullOrEmpty(request.createdAtStart))
                {
                    whereConditions.Add("u.created_at >= @CreatedAtStart");
                    parameters.Add("CreatedAtStart", request.createdAtStart);
                }

                // Фильтрация по дате создания (конец)
                if (!string.IsNullOrEmpty(request.createdAtEnd))
                {
                    whereConditions.Add("u.created_at <= @CreatedAtEnd");
                    parameters.Add("CreatedAtEnd", request.createdAtEnd);
                }

                // Фильтрация по дате обновления (начало)
                if (!string.IsNullOrEmpty(request.updatedAtStart))
                {
                    whereConditions.Add("u.updated_at >= @UpdatedAtStart");
                    parameters.Add("UpdatedAtStart", request.updatedAtStart);
                }

                // Фильтрация по дате обновления (конец)
                if (!string.IsNullOrEmpty(request.updatedAtEnd))
                {
                    whereConditions.Add("u.updated_at <= @UpdatedAtEnd");
                    parameters.Add("UpdatedAtEnd", request.updatedAtEnd);
                }

                string whereClause = whereConditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", whereConditions)}"
                    : "";
                // Валидация и настройка сортировки
                var validSortColumns = new Dictionary<string, string>
                {
                    ["id"] = "u.id",
                    ["name"] = "u.name",
                    ["email"] = "u.email",
                    ["phone"] = "u.phone",
                    ["role"] = "u.role",
                    ["createdat"] = "u.created_at",
                    ["updatedat"] = "u.updated_at"
                };

                var sortColumnKey = request.sortBy?.ToLower();
                string sortColumn = "u.created_at";

                if (sortColumnKey is not null && validSortColumns.ContainsKey(sortColumnKey))
                    sortColumn = validSortColumns[sortColumnKey];

                string orderByClause = $"ORDER BY {sortColumn} {request.sortType ?? "DESC"}";

                // SQL для получения общего количества записей
                var countSql = $@"
                    SELECT COUNT(DISTINCT u.id)
                    FROM USER u
                    LEFT JOIN USER creator ON u.updated_by = creator.id
                    LEFT JOIN adress a ON a.user_id = u.id
                    {whereClause}";

                int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                int totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);

                // SQL для получения данных с пагинацией
                var sql = $@"
                    SELECT
                        u.id,
                        u.name,
                        u.email,
                        u.phone,
                        u.role,
                        u.is_deleted as isDeleted,
                        DATE_FORMAT(u.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(u.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt,
                        u.updated_by,
                        creator.id as creator_id,
                        creator.name as creator_name,
                        creator.role as creator_type,
                        a.id as adressId,
                        a.city as adressCity,
                        a.street as adressStreet,
                        a.house as adressHouse,
                        a.apartment as adressApartment,
                        DATE_FORMAT(a.created_at, '%Y-%m-%dT%H:%i:%s') as adressCreatedAt,
                        DATE_FORMAT(a.updated_at, '%Y-%m-%dT%H:%i:%s') as adressUpdatedAt
                    FROM USER u
                    LEFT JOIN USER creator ON u.updated_by = creator.id
                    LEFT JOIN adress a ON a.user_id = u.id
                    {whereClause}
                    {orderByClause}
                    LIMIT @PageSize OFFSET @Offset";

                parameters.Add("PageSize", request.pageSize);
                parameters.Add("Offset", offset);

                var userCache = new Dictionary<long, UserResponse>();

                var rows = await connection.QueryAsync(sql, parameters);

                foreach (var row in rows)
                {
                    long userId = row.id;

                    if (!userCache.TryGetValue(userId, out var userResponse))
                    {
                        userResponse = new UserResponse(
                            id: userId,
                            name: row.name,
                            email: row.email,
                            phone: row.phone,
                            role: row.role,
                            isDeleted: row.isDeleted,
                            createdAt: row.createdAt,
                            updatedAt: row.updatedAt,
                            adresses: new List<Adress>(),
                            updatedBy: row.creator_id is not null
                                ? new UserShort { id = row.creator_id, name = row.creator_name, type = row.creator_type }
                                : null
                        );
                        userCache[userId] = userResponse;
                    }

                    if (row.adressId is not null)
                    {
                        userResponse.adresses.Add(new Adress
                        {
                            id = row.adressId,
                            user = new UserShort { id = userId, name = row.name, type = row.role },
                            city = row.adressCity,
                            street = row.adressStreet,
                            house = row.adressHouse,
                            apartment = row.adressApartment,
                            createdAt = row.adressCreatedAt,
                            updatedAt = row.adressUpdatedAt
                        });
                    }
                }

                var users = userCache.Values.ToList();

                return new UsersResponse(
                    users,
                    totalCount,
                    request.pageNumber,
                    request.pageSize,
                    totalPages
                );
            }
        }

        public async Task<UserResponse> UpdateUser(long id, UserUpdateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                // Проверка существования пользователя
                var userExistsSql = "SELECT COUNT(*) FROM user WHERE id = @id    AND is_deleted = false";
                var userExists = await connection.ExecuteScalarAsync<int>(userExistsSql, new { id });

                if (userExists == 0)
                    throw new KeyNotFoundException($"Пользователь не найден по заданному Id = {id}");

                // Проверка уникальности только для полей, переданных в запросе
                if (request.email is not null)
                {
                    var emailExistsSql = @"SELECT count(*) FROM user WHERE email = @email AND id != @id AND is_deleted = false";
                    var emailExists = await connection.ExecuteScalarAsync<int>(emailExistsSql, new { request.email, id });

                    if (emailExists > 0)
                        throw new ArgumentException("Email уже используется другим пользователем");
                }

                if (request.phone is not null)
                {
                    var phoneExistsSql = @"SELECT count(*) FROM user WHERE phone = @phone AND id != @id AND is_deleted = false";
                    var phoneExists = await connection.ExecuteScalarAsync<int>(phoneExistsSql, new { request.phone, id });

                    if (phoneExists > 0)
                        throw new ArgumentException("Данный номер телефона уже используется другим пользователем");
                }

                // Обновляем только те поля, которые заполнены в запросе (не null)
                var setClauses = new List<string>();
                var parameters = new DynamicParameters();

                if (request.name is not null)
                {
                    setClauses.Add("name = @name");
                    parameters.Add("name", request.name);
                }

                if (request.email is not null)
                {
                    setClauses.Add("email = @email");
                    parameters.Add("email", request.email);
                }

                if (request.phone is not null)
                {
                    setClauses.Add("phone = @phone");
                    parameters.Add("phone", request.phone);
                }

                if (request.password is not null)
                {
                    setClauses.Add("password_hash = @passwordHash");
                    parameters.Add("passwordHash", HashPassword(request.password));
                }

                if (setClauses.Count == 0)
                    throw new ArgumentException("Не указано ни одного поля для обновления");

                setClauses.Add("updated_by = @updatedBy");
                parameters.Add("updatedBy", parsed.Value.userId);
                parameters.Add("userId", id);

                var sql = $@"
                    UPDATE USER
                    SET {string.Join(", ", setClauses)}
                    WHERE id = @userId AND is_deleted = false";

                int affectedRows = await connection.ExecuteAsync(sql, parameters);

                return await GetUserSql(id, connection, true);
            }
        }

        public async Task<UserResponse> CreateUser(UserCreateRequest request, string? token)
        {
            (long userId, string userRole)? parsed = default;

            if (token is not null)
            {
                UserRole[] roles = { UserRole.Admin };
                parsed = await SessionService.CheckGrants(token, roles);
            }

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                long newUserId = 0;

                if (request.email is not null)
                {
                    // Проверка уникальности email
                    var emailExistsSql = @"SELECT count(*) FROM user WHERE email = @email AND is_deleted = false";
                    var emailExists = await connection.ExecuteScalarAsync<int>(emailExistsSql, new { request.email });

                    if (emailExists > 0)
                        throw new ArgumentException("Email уже используется другим пользователем");
                }

                var phoneExistsSql = @"SELECT count(*) FROM user WHERE phone = @phone AND is_deleted = false";
                var phoneExists = await connection.ExecuteScalarAsync<int>(phoneExistsSql, new { request.phone });

                if (phoneExists > 0)
                    throw new ArgumentException("Данный номер телефона уже используется другим пользователем");

                if (token is null)
                {
                    var sql = @"
                    INSERT INTO USER (name, email, phone, password_hash, role)
                    VALUES (@name, @email, @phone, @passwordHash, @role);
                    SELECT LAST_INSERT_ID();";

                    var parameters = new
                    {
                        request.name,
                        request.email,
                        request.phone,
                        passwordHash = HashPassword(request.password),
                        role = UserRole.Customer.Code
                    };

                    newUserId = await connection.ExecuteScalarAsync<long>(sql, parameters);
                }

                if (token is not null)
                {
                    var sql = @"
                    INSERT INTO USER (name, email, phone, password_hash, role, updated_by)
                    VALUES (@name, @email, @phone, @passwordHash, @role, @updatedBy);
                    SELECT LAST_INSERT_ID();";

                    var parameters = new
                    {
                        request.name,
                        request.email,
                        request.phone,
                        passwordHash = HashPassword(request.password),
                        request.role,
                        updatedBy = parsed.Value.userId
                    };

                    newUserId = await connection.ExecuteScalarAsync<long>(sql, parameters);
                }

                return await GetUserSql(newUserId, connection, true);
            }
        }

        public async Task<bool> DeleteUser(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                // Проверяем существование пользователя
                var userExistsSql = "SELECT COUNT(*) FROM user WHERE id = @userId AND is_deleted = false";
                var userExists = await connection.ExecuteScalarAsync<int>(userExistsSql, new { id });

                if (userExists == 0)
                    throw new KeyNotFoundException("Пользователь не найден или уже удален");

                // Выполняем soft delete (помечаем как удаленного)
                var deleteSql = @"
                    UPDATE user 
                    SET is_deleted = true,
                        token_hash = NULL
                    WHERE id = @userId";

                int affectedRows = await connection.ExecuteAsync(deleteSql, new { UserId = id });

                return (affectedRows > 0);
            }
        }


    }
}

