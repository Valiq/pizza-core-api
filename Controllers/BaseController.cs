using Microsoft.AspNetCore.Mvc;

namespace pizza_core_api.Controllers
{
    [ApiController]
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Токен из заголовка Authorization.
        /// Возвращает null, если заголовок отсутствует, не является Bearer-заголовком
        /// или сам токен пустой, — так анонимный запрос отличается от запроса с токеном.
        /// </summary>
        protected string? GetBearerToken()
        {
            string? auth = Request.Headers.Authorization.ToString();

            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            string token = auth["Bearer ".Length..].Trim();

            return string.IsNullOrEmpty(token) ? null : token;
        }

        /// <summary>
        /// Токен для эндпоинтов с [Authorize]: middleware аутентификации уже проверил заголовок,
        /// поэтому отсутствие токена здесь трактуется как отсутствие авторизации (401).
        /// </summary>
        protected string GetRequiredBearerToken()
        {
            return GetBearerToken() ?? throw new UnauthorizedAccessException("Отсутствует заголовок Authorization с Bearer-токеном");
        }
    }
}