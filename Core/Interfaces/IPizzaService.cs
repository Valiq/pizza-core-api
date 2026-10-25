using MySqlConnector;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface IPizzaService
    {
        Task<Pizza> GetPizza(long id, string? token);
        Task<Pizza> GetPizzaSql(long id, MySqlConnection connection, bool isAdmin);
        Task<PizzasResponse> GetPizzasPaged(PizzaFilterRequest request, string? token);
        Task<Pizza> CreatePizza(PizzaCreateRequest request, string token);
        Task<Pizza> UpdatePizza(long id, PizzaUpdateRequest request, string token);
        Task<bool> DeletePizza(long id, string token);
    }
}
