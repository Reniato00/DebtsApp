using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace debts.api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
                options.MaxModelBindingCollectionSize = 1024;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwagger();
            services.AddMemoryCache();
            services.AddApiRateLimiting(configuration);
            services.AddApiCors(configuration);

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 102_400;
            });

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var secret = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secret)
                };
            });

            return services;
        }

        private static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Debts API",
                    Version = "v1",
                    Description = "API para gestión de deudas, pagos y reportes financieros multi-usuario."
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        private static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", policy =>
                {
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }

        private static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var permitLimit = int.TryParse(configuration["RateLimiting:PermitLimit"], out var limit) ? limit : 100;
            var windowSeconds = int.TryParse(configuration["RateLimiting:WindowSeconds"], out var window) ? window : 60;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("UserBased", context =>
                {
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var partitionKey = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowSeconds)
                        });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    var response = new
                    {
                        statusCode = 429,
                        message = "Too many requests. Try again later.",
                        timestamp = DateTime.UtcNow
                    };
                    await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
                };
            });

            return services;
        }
    }
}
