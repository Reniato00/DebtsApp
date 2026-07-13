using services.Services;
using Microsoft.Extensions.DependencyInjection;

namespace services.Extensions
{
    public static class ServiceCollectorExtensions
    {
        public static void AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IDebtService, DebtService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICalculatorService, CalculatorService>();
        }
    }
}
