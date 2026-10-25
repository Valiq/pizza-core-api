using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/adresses")]
    public class AdressesController : BaseController
    {
        private readonly IAdressService Service;

        public AdressesController(IAdressService service)
        {
            Service = service;
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Adress), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdress([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            Adress result = await Service.GetAdress(id, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Adress), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAdress([FromBody] AdressCreateRequest request)
        {
            string token = GetRequiredBearerToken();
            Adress result = await Service.CreateAdress(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Adress), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAdress([FromRoute] long id, [FromBody] AdressUpdateRequest request)
        {
            string token = GetRequiredBearerToken();
            Adress result = await Service.UpdateAdress(id, request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdress([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            bool result = await Service.DeleteAdress(id, token);
            return Ok(result);
        }
    }
}
