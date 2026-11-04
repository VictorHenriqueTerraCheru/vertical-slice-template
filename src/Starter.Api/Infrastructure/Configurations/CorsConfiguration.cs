using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class CorsConfiguration
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        services.AddCors(options => options.AddPolicy(CorsPolicies.Default, policy => policy
            .WithOrigins([.. configuration.CorsOrigins])
            .AllowAnyHeader()
            .AllowAnyMethod()));

    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(CorsPolicies.Default);
        return app;
    }
}
