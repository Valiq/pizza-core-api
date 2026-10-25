using pizza_core_api.Core.Models;
using System.Data;

namespace pizza_core_api.Core.Records
{
    public class Respons
    {
        public record UserResponse(
            long id,
            string name,
            string? email,
            string phone,
            string role,
            bool isDeleted,
            string createdAt,
            string updatedAt,
            List<Adress>? adresses,
            UserShort? updatedBy
        );

        public record UsersResponse(
            List<UserResponse> Users,
            int TotalCount,
            int PageNumber,
            int PageSize,
            int TotalPages
        );

        public record PizzasResponse(
            List<Pizza> Pizzas,
            int TotalCount,
            int PageNumber,
            int PageSize,
            int TotalPages
        );

        public record IngredientsResponse(
            List<Ingredient> Ingredients,
            int TotalCount,
            int PageNumber,
            int PageSize,
            int TotalPages
        );

        public record OrderResponse(
            long id,
            UserShort user,
            Adress adress,
            List<OrderPizza> orderPizzas,
            decimal totalPrice,
            string status,
            string paymentType,
            string? comment,
            string createdAt,
            string updatedAt
        );

        public record OrdersResponse(
            List<Order> Orders,
            int TotalCount,
            int PageNumber,
            int PageSize,
            int TotalPages
        );
    }
}
