using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace pizza_core_api.Swagger
{
    /// <summary>
    /// Ставит замочек Bearer только тем методам, где есть [Authorize].
    /// Остальные методы вызываются без токена.
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            bool hasAllowAnonymous = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any();

            bool hasAuthorize =
                (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true) ||
                context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (!hasAuthorize || hasAllowAnonymous)
            {
                // Метод открытый, но токен можно приложить (например CreateUser:
                // регистрация без токена, создание админом — с токеном).
                // Пустой объект в security означает "авторизация необязательна".
                if ((hasAllowAnonymous && context.MethodInfo.Name == nameof(Controllers.UsersController.CreateUser)) ||
                         (hasAllowAnonymous && context.MethodInfo.Name == nameof(Controllers.PizzasController.GetPizza)) ||
                          (hasAllowAnonymous && context.MethodInfo.Name == nameof(Controllers.PizzasController.GetAllPizzas)) ||
                           (hasAllowAnonymous && context.MethodInfo.Name == nameof(Controllers.IngredientsController.GetIngredient)) ||
                            (hasAllowAnonymous && context.MethodInfo.Name == nameof(Controllers.IngredientsController.GetAllIngredients)))
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                    });
                    operation.Security.Add(new OpenApiSecurityRequirement());
                }

                return;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                // Конструктор требует ссылку на документ для резолва схемы "Bearer"
                // из components/securitySchemes (объявлена через AddSecurityDefinition).
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }
    }
}
