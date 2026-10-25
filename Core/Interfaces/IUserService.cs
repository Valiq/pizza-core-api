using MySqlConnector;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse> GetUser(long id, string token);
        Task<UserResponse> GetUserSql(long id, MySqlConnection connection, bool isAdmin);
        Task<UsersResponse> GetUsersPaged(UserFilterRequest request, string token);
        Task<UserResponse> UpdateUser(long userId, UserUpdateRequest request, string token);
        Task<UserResponse> CreateUser(UserCreateRequest request, string? token);
        Task<bool> DeleteUser(long userId, string token);
    }
}
