using Microsoft.Extensions.DependencyInjection;
using persistence.Connection;
using persistence.Repositories;

namespace persistence.Extensions
{
    public static class ServiceCollectorExtensions
    {
        public static void AddPersistence(this IServiceCollection services)
        {
            services.AddScoped<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IDebtRepository, DebtRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        }
    }
}
