using MySqlConnector;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> GetOrder(long id, string token);
        Task<OrdersResponse> GetOrdersPaged(OrderFilterRequest request, string token);
        Task<OrderResponse> GetOrderSql(long id, MySqlConnection connection);
        Task<OrderResponse> CreateOrder(OrderCreateRequest request, string token);
        Task<OrderResponse> UpdateOrder(long id, OrderUpdateRequest request, string token);
    }
}