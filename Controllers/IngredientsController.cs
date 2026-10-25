using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/ingredients")]
    public class IngredientsController : BaseController
    {
        private readonly IIngredientService Service;

        public IngredientsController(IIngredientService service)
        {
            Service = service;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(IngredientsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllIngredients([FromQuery] IngredientFilterRequest request)
        {
            string? token = GetBearerToken();
            IngredientsResponse result = await Service.GetIngredientsPaged(request, token);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Ingredient), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIngredient([FromRoute] long id)
        {
            string? token = GetBearerToken();
            Ingredient result = await Service.GetIngredient(id, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Ingredient), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateIngredient([FromBody] IngredientCreateRequest request)
        {
            string token = GetRequiredBearerToken();
            Ingredient result = await Service.CreateIngredient(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Ingredient), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateIngredient([FromRoute] long id, [FromBody] IngredientUpdateRequest request)
        {
            string token = GetRequiredBearerToken();
            Ingredient result = await Service.UpdateIngredient(id, request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIngredient([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            bool result = await Service.DeleteIngredient(id, token);
            return Ok(result);
        }
    }
}
