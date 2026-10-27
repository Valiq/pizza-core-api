using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public class UsersController : BaseController
    {
        private readonly IUserService Service;

        public UsersController(IUserService service)
        {
            Service = service;
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(UsersResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllUser([FromQuery] UserFilterRequest request)
        {
            string token = GetRequiredBearerToken();
            UsersResponse result = await Service.GetUsersPaged(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUser([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            UserResponse result = await Service.GetUser(id, token);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateRequest request)
        {
            string? token = GetBearerToken();
            UserResponse result = await Service.CreateUser(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUser([FromRoute] long id, [FromBody] UserUpdateRequest request)
        {
            string token = GetRequiredBearerToken();
            UserResponse result = await Service.UpdateUser(id, request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            bool result = await Service.DeleteUser(id, token);
            return Ok(result);
        }

    }
}
