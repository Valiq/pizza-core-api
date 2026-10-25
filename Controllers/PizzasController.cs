using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/pizzas")]
    public class PizzasController : BaseController
    {
        private readonly IPizzaService Service;

        public PizzasController(IPizzaService service)
        {
            Service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PizzasResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPizzas([FromQuery] PizzaFilterRequest request)
        {
            string? token = GetBearerToken();
            PizzasResponse result = await Service.GetPizzasPaged(request, token);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Pizza), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPizza(long id)
        {
            string? token = GetBearerToken();
            Pizza result = await Service.GetPizza(id, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Pizza), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePizza([FromBody] PizzaCreateRequest request)
        {
            string token = GetRequiredBearerToken();
            Pizza result = await Service.CreatePizza(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Pizza), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePizza(long id, [FromBody] PizzaUpdateRequest request)
        {
            string token = GetRequiredBearerToken();
            Pizza result = await Service.UpdatePizza(id, request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePizza(long id)
        {
            string token = GetRequiredBearerToken();
            bool result = await Service.DeletePizza(id, token);
            return Ok(result);
        }

    }
}
