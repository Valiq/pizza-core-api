using Dapper;
using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using System.Data;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class AdressService : IAdressService
    {
        protected readonly IConfiguration Configuration;
        private readonly ISessionService SessionService;

        public AdressService(IConfiguration configuration, ISessionService sessionService)
        {
            Configuration = configuration;
            SessionService = sessionService;
        }

        public async Task<Adress> GetAdress(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var adress = await AdressSql(id, connection);

                if (parsed.Value.userRole != UserRole.Admin.Code && parsed.Value.userId != adress.user.id)
                    throw new InvalidOperationException($"Данный адрес не принадлежит вам");

                return adress;
            }
        }

        public async Task<Adress> AdressSql(long id, MySqlConnection connection)
        {
            var sql = @"
                    SELECT 
                        a.id,
                        a.city,
                        a.street,
                        a.house,
                        a.apartment,
                        DATE_FORMAT(a.created_at, '%Y-%m-%dT%H:%i:%s') as createdAt,
                        DATE_FORMAT(a.updated_at, '%Y-%m-%dT%H:%i:%s') as updatedAt,
                        u.id,
                        u.name,
                        u.role as type
                    FROM adress a
                    LEFT JOIN user u ON a.user_id = u.id
                    WHERE a.id = @id";

            var adress = (await connection.QueryAsync<Adress, UserShort, Adress>(
                sql,
                (adress, user) =>
                {
                    return new Adress
                    {
                        id = adress.id,
                        user = user,
                        city = adress.city,
                        street = adress.street,
                        house = adress.house,
                        apartment = adress.apartment,
                        createdAt = adress.createdAt,
                        updatedAt = adress.updatedAt
                    };
                },
                new { id },
                splitOn: "id"
            )).FirstOrDefault();

            if (adress is null)
                throw new KeyNotFoundException($"Адрес с идентификатором {id} не найден");

            return adress;
        }

        public async Task<Adress> CreateAdress(AdressCreateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO adress (user_id, city, street, house, apartment)
                    VALUES (@userId, @city, @street, @house, @apartment);
                    SELECT LAST_INSERT_ID();";

                var parameters = new
                {
                    request.userId,
                    request.city,
                    request.street,
                    request.house,
                    request.apartment
                };

                long newAdressId = await connection.ExecuteScalarAsync<long>(sql, parameters);

                return await AdressSql(newAdressId, connection);
            }
        }

        public async Task<Adress> UpdateAdress(long id, AdressUpdateRequest request, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var adressExistsSql = "SELECT COUNT(*) FROM adress WHERE id = @id";
                var adressExists = await connection.ExecuteScalarAsync<int>(adressExistsSql, new { id });

                if (adressExists == 0)
                    throw new KeyNotFoundException($"Адрес с идентификатором {id} не найден");

                if (parsed.Value.userRole != UserRole.Admin.Code)
                {
                    var ownerSql = "SELECT user_id FROM adress WHERE id = @id";
                    var ownerUserId = await connection.ExecuteScalarAsync<long>(ownerSql, new { id });

                    if (parsed.Value.userId != ownerUserId)
                        throw new InvalidOperationException($"Данный адрес не принадлежит вам");
                }

                var parameters = new
                {
                    id,
                    request.city,
                    request.street,
                    request.house,
                    request.apartment
                };

                var sql = @"
                    UPDATE adress 
                    SET city = @city, 
                        street = @street, 
                        house = @house, 
                        apartment = @apartment
                    WHERE id = @id";

                await connection.ExecuteAsync(sql, parameters);

                return await GetAdress(id, token);
            }
        }

        public async Task<bool> DeleteAdress(long id, string token)
        {
            UserRole[] roles = { UserRole.Admin, UserRole.Customer };

            var parsed = await SessionService.CheckGrants(token, roles);

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var adressExistsSql = "SELECT COUNT(*) FROM adress WHERE id = @id";
                var adressExists = await connection.ExecuteScalarAsync<int>(adressExistsSql, new { id });

                if (adressExists == 0)
                    throw new KeyNotFoundException($"Адрес с идентификатором {id} не найден");

                if (parsed.Value.userRole != UserRole.Admin.Code)
                {
                    var ownerSql = "SELECT user_id FROM adress WHERE id = @id";
                    var ownerUserId = await connection.ExecuteScalarAsync<long>(ownerSql, new { id });

                    if (parsed.Value.userId != ownerUserId)
                        throw new InvalidOperationException($"Данный адрес не принадлежит вам");
                }

                var deleteSql = "DELETE FROM adress WHERE id = @id";

                int affectedRows = await connection.ExecuteAsync(deleteSql, new { id });

                return (affectedRows > 0);
            }
        }
    }
}
