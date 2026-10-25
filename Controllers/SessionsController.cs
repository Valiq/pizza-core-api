using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/sessions")]
    public class SessionsController : BaseController
    {
        private readonly ISessionService Service;

        public SessionsController(ISessionService service)
        {
            Service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] LoginRequest request)
        {
            string token = await Service.CreateSession(request);
            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("current/user")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserByToken()
        {
            // [Authorize] уже проверил JWT через AddJwtBearer.
            // Токен из заголовка нужен только для сверки с token_hash в БД.
            string token = GetRequiredBearerToken();

            UserResponse? user = await Service.GetUserByToken(token);

            if (user is null)
                return Unauthorized();

            return Ok(user);
        }

        [Authorize]
        [HttpDelete("current")]
        public async Task<IActionResult> Logout()
        {
            string token = GetRequiredBearerToken();
            await Service.Logout(token);

            return NoContent();
        }

    }
}

