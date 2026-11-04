using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Common;
using Starter.Api.Health;
using Starter.Api.Persistence;

namespace Starter.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        services
            .AddErrorHandling()
            .AddJson()
            .AddClock()
            .AddValidation()
            .AddPersistence(configuration)
            .AddRepositories()
            .AddServices();

    private static IServiceCollection AddClock(this IServiceCollection services) =>
        services.AddSingleton<TimeProvider>(new PostgresClock());

    private static IServiceCollection AddValidation(this IServiceCollection services)
    {
        ValidationExtensions.ConfigureGlobal();
        return services.AddValidatorsFromAssemblyContaining<Program>();
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        services
            .AddDbContext<AppDbContext>(options => options
                .UseNpgsql(configuration.DatabaseConnectionString)
                .UseSnakeCaseNamingConvention())
            .AddScoped<IUnitOfWork, UnitOfWork>();

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IHealthRepository, HealthRepository>();
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        return services;
    }
}
