using pizza_core_api.Core.Enums;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSession(LoginRequest request);
        Task<UserResponse?> GetUserByToken(string token);
        Task Logout(string token);
        Task<(long userId, string userRole)?> CheckGrants(string token, UserRole[] roles);
    }
}

