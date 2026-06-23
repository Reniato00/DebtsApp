using debts.api.Middlewares;

namespace debts.api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            app.UseMiddleware<ExceptionMiddleware>();

            app.UseSecurityHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowedOrigins");
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();
            app.MapControllers();

            return app;
        }

        private static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-Frame-Options", "DENY");
                context.Response.Headers.Append("Referrer-Policy", "no-referrer");
                context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
                context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

                if (!app.Environment.IsDevelopment())
                {
                    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
                }

                await next();
            });

            return app;
        }
    }
}
