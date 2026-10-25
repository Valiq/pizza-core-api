using Dapper;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using pizza_core_api.Core.Enums;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using static BCrypt.Net.BCrypt;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Services
{
    public class SessionService : ISessionService
    {
        protected readonly IConfiguration Configuration;
        private readonly IServiceProvider _serviceProvider;

        public SessionService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            Configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        public async Task<(long userId, string userRole)?> CheckGrants(string token, UserRole[] roles)
        {
            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                var parsed = GetUserFromToken(token);

                string[] rolesStr = roles.Select(x => x.Code).ToArray();

                if (parsed is not null && rolesStr.Contains(parsed.Value.userRole))
                    return parsed;

                throw new UnauthorizedAccessException("У вас недостаточно прав");
            }
        }

        public async Task<string> CreateSession(LoginRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                const string sql = @"
                        SELECT
                            id,
                            name,
                            email,
                            phone,
                            role,
                            password_hash as passwordHash,
                            token_hash as tokenHash
                        FROM user 
                        WHERE phone = @phone AND is_deleted = false";

                User? user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { request.phone });

                if (user is null)
                    throw new UnauthorizedAccessException("Пользователь не найден");

                if (!Verify(request.password, user.passwordHash))
                    throw new UnauthorizedAccessException("Неверный номер телефона или пароль");

                string token = GenerateJwtToken(user);

                const string updateSql = @"UPDATE user SET token_hash = @tokenHash WHERE id = @id";
                await connection.ExecuteAsync(updateSql, new { tokenHash = HashPassword(token), user.id });

                return token;
            }
        }

        public async Task<UserResponse?> GetUserByToken(string token)
        {
            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                User? user = await GetUserByToken(connection, token);

                if (user is null)
                    return null;

                var userService = _serviceProvider.GetRequiredService<IUserService>();
                return await userService.GetUserSql(user.id, connection, true);
            }
        }

        internal async Task<User?> GetUserByToken(MySqlConnection connection, string token)
        {
            var parsed = GetUserFromToken(token);

            if (parsed is null)
                return null;

            long userId = parsed.Value.userId;

            const string sql = @"SELECT
                            id,
                            name,
                            email,
                            phone,
                            role,
                            password_hash as passwordHash,
                            token_hash as tokenHash,
                            is_deleted as isDeleted,
                            DATE_FORMAT(created_at, '%Y-%m-%dT%H:%i:%s') as createdAt
                        FROM user WHERE id = @Id AND is_deleted = false";
            User? user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = userId });

            if (user is null)
                throw new KeyNotFoundException($"Пользователь не найден или удалён");

            // Токен отозван (logout или новый логин перезаписал хеш)
            if (string.IsNullOrEmpty(user.tokenHash) || !Verify(token, user.tokenHash))
                return null;

            return user;
        }

        public async Task Logout(string token)
        {
            var parsed = GetUserFromToken(token);

            if (parsed is null)
                return;

            long userId = parsed.Value.userId;

            using (MySqlConnection connection = new MySqlConnection(Configuration.GetValue<string>("ConnectionString")))
            {
                await connection.OpenAsync();

                const string sql = @"UPDATE user SET token_hash = NULL WHERE id = @Id";
                await connection.ExecuteAsync(sql, new { Id = userId });
            }
        }

        private static (long userId, string userRole)? GetUserFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwt = handler.ReadJwtToken(token);

                if (long.TryParse(jwt.Subject, out long userId))
                {
                    string? role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                        ?? jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                    return (userId, role);
                }
            }
            catch
            {
                Console.WriteLine("Нет JWT или JWT повреждён/невалиден");
            }

            return null;
        }

        private string GenerateJwtToken(User user)
        {
            string key = Configuration.GetValue<string>("Jwt:Key")
                ?? throw new InvalidOperationException("Jwt:Key не настроен в appsettings.json");
            string issuer = Configuration.GetValue<string>("Jwt:Issuer") ?? "PizzaCore";
            string audience = Configuration.GetValue<string>("Jwt:Audience") ?? "PizzaCoreClient";
            int expireMinutes = Configuration.GetValue<int?>("Jwt:ExpireMinutes") ?? 1440;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.id.ToString()),
                new Claim(JwtRegisteredClaimNames.PhoneNumber, user.phone ?? string.Empty),
                new Claim(ClaimTypes.Name, user.name ?? string.Empty),
                new Claim(ClaimTypes.Role, user.role ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
