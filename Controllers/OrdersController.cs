using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Controllers
{
    [ApiController]
    [Route("api/v1/orders")]
    public class OrdersController : BaseController
    {
        private readonly IOrderService Service;

        public OrdersController(IOrderService service)
        {
            Service = service;
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(OrdersResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest request)
        {
            string token = GetRequiredBearerToken();
            OrdersResponse result = await Service.GetOrdersPaged(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrder([FromRoute] long id)
        {
            string token = GetRequiredBearerToken();
            OrderResponse result = await Service.GetOrder(id, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
        {
            string token = GetRequiredBearerToken();
            OrderResponse result = await Service.CreateOrder(request, token);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateOrder([FromRoute] long id, [FromBody] OrderUpdateRequest request)
        {
            string token = GetRequiredBearerToken();
            OrderResponse result = await Service.UpdateOrder(id, request, token);
            return Ok(result);
        }
    }
}
