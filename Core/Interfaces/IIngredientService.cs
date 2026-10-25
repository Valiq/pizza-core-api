using MySqlConnector;
using pizza_core_api.Core.Models;
using static pizza_core_api.Core.Records.Requests;
using static pizza_core_api.Core.Records.Respons;

namespace pizza_core_api.Core.Interfaces
{
    public interface IIngredientService
    {
        Task<Ingredient> GetIngredient(long id, string? token);
        Task<Ingredient> IngredientSql(long id, MySqlConnection connection, bool isAdmin);
        Task<IngredientsResponse> GetIngredientsPaged(IngredientFilterRequest request, string? token);
        Task<Ingredient> UpdateIngredient(long id, IngredientUpdateRequest request, string token);
        Task<Ingredient> CreateIngredient(IngredientCreateRequest request, string token);
        Task<bool> DeleteIngredient(long id, string token);
    }
}