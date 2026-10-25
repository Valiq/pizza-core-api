using pizza_core_api.Core.Interfaces;
using pizza_core_api.Core.Services;

namespace pizza_core_api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "PizzaCore API", Version = "v1" });

                // Только кнопка Authorize. Замочек появляется лишь на методах с [Authorize].
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
                {
                    Description = "Вставь JWT токен (можно оставить пустым для открытых методов)",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                c.OperationFilter<Swagger.AuthorizeCheckOperationFilter>();
            });

            builder.Services.AddScoped<IPizzaService, PizzaService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IIngredientService, IngridientService>();
            builder.Services.AddScoped<IAdressService, AdressService>();
            builder.Services.AddScoped<IOrderService, OrderService>();

            builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Включаем подробный ответ JwtBearer в логах, чтобы видеть причину 401
                    options.IncludeErrorDetails = true;
                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");
                            logger.LogWarning(ctx.Exception, "JWT rejected: {Message}", ctx.Exception.Message);
                            return Task.CompletedTask;
                        }
                    };
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
                        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience"),
                        ClockSkew = TimeSpan.FromMinutes(1),
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(
                                builder.Configuration.GetValue<string>("Jwt:Key") ?? throw new InvalidOperationException("Jwt:Key не настроен")))
                    };
                });

            var app = builder.Build();

            app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PizzaCore API v1");
                c.RoutePrefix = "swagger";
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
