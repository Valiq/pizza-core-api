using Microsoft.AspNetCore.Http.Connections;
using pizza_core_api.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace pizza_core_api.Core.Records
{
    public class Requests
    {
        public record LoginRequest(
           [Phone]
           [Required]
           [RegularExpression(@"^\+375\d{9}$", ErrorMessage = "Формат: +375XXXXXXXXX")]
           string phone,

           [Required]
           [MinLength(6)]
           [MaxLength(50)]
           string password
        );

        public record UserFilterRequest(
            string? email,
            string? name,
            string? phone,
            string[]? role,
            bool? isDeleted,
            string? createdAtStart,
            string? createdAtEnd,
            string? updatedAtStart,
            string? updatedAtEnd,
            string? createdBy,
            string? adress,

            string? sortBy,
            string? sortType,

            [Required]
            int pageNumber = 1,

            [Required]
            int pageSize = 50
        );

        public record UserCreateRequest(
            [Required]
            [MinLength(3)]
            [MaxLength(100)]
            string name,

            [EmailAddress]
            [MaxLength(255)]
            string? email,

            [Phone]
            [Required]
            [RegularExpression(@"^\+375\d{9}$", ErrorMessage = "Формат: +375XXXXXXXXX")]
            string? phone,

            [Required]
            [MinLength(8)]
            [MaxLength(255)]
            string password,

            [Required]
            string role
        );

        public record UserUpdateRequest(
            [MinLength(3)]
            [MaxLength(100)]
            string? name,

            [EmailAddress]
            [MaxLength(255)]
            string? email,

            [Phone]
            [RegularExpression(@"^\+375\d{9}$" , ErrorMessage = "Формат: +375XXXXXXXXX")]
            string? phone,

            [MinLength(8)]
            [MaxLength(255)]
            string? password
        );

        public record PizzaCreateRequest(
            [Required]
            [MinLength(3)]
            [MaxLength(100)]
            string name,
            [Required]
            string type,
            [Required]
            decimal basePrice,
            string? imagePath,
            [Required]
            long[] ingredients
        );

        public record PizzaUpdateRequest(
            [MinLength(3)]
            [MaxLength(100)]
            string name,
            string? type,
            decimal? basePrice,
            string? imagePath,
            long[]? ingredients
        );

        public record PizzaFilterRequest(
            string? name,
            string? type,
            bool? isDeleted,
            string? createdAtStart,
            string? createdAtEnd,
            string? sortBy,
            string? sortType,
            long[]? ingredients,
            [Required]
            int pageNumber = 1,
            [Required]
            int pageSize = 50
        );

        public record IngredientFilterRequest(
            string? name,
            string? type,
            bool? isDeleted,
            string? createdAtStart,
            string? createdAtEnd,
            string? sortBy,
            string? sortType,
            [Required]
            int pageNumber = 1,

            [Required]
            int pageSize = 50
        );

        public record IngredientCreateRequest(
            [Required]
            [MinLength(3)]
            [MaxLength(100)]
            string name,

            [Required]
            string type,

            [Required]
            decimal basePrice
        );

        public record IngredientUpdateRequest(
            [MinLength(3)]
            [MaxLength(100)]
            string name,

            string? type,

            decimal? basePrice
        );

        public record AdressCreateRequest(
            [Required]
            long userId,
            
            [Required]
            [MinLength(3)]
            [MaxLength(100)]
            string city,

            [Required]
            [MinLength(3)]
            [MaxLength(100)]
            string street,

            [Required]
            [MinLength(1)]
            [MaxLength(20)]
            string house,

            string? apartment
        );

        public record AdressUpdateRequest(
            [MinLength(3)]
            [MaxLength(100)]
            string city,

            [MinLength(3)]
            [MaxLength(100)]
            string street,

            [MinLength(1)]
            [MaxLength(20)]
            string house,

            string? apartment
        );

        public record OrderFilterRequest(
            string? status,
            string? paymentType,
            decimal? totalPriceFrom,
            decimal? totalPriceTo,
            string? divisionType,
            string? pizzaSize,
            string? pizzaName,
            string? createdAtStart,
            string? createdAtEnd,
            string? sortBy,
            string? sortType,
            [Required]
            int pageNumber = 1,
            [Required]
            int pageSize = 50
        );

        public record OrderCreateRequest(
            [Required]
            long adressId,
            List<OrderPizzaCreateRequest> orderPizzas,
            string? paymentType,
            string? comment
        );

        public record OrderPizzaCreateRequest(
            [Required]
            long pizzaId,
            string divisionType,
            string doughType,
            string edgeType,
            string size,
            List<long> additionalIngredientIds
        );

        public record OrderUpdateRequest(
            string? status
        );

    }
}
