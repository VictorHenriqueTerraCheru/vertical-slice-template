using System.Net.Http.Headers;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using Starter.Api.AiUsage;
using Starter.Api.Assistant;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Health;
using Starter.Api.Persistence;
using Starter.Api.Todos;
using Starter.Api.Users;

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
            .AddServices()
            .AddEmailSender(configuration)
            .AddChatModel(configuration);

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

    public static IServiceCollection AddEmailSender(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        configuration.Environment == EnvironmentNames.Testing
        || (configuration.Environment == EnvironmentNames.Development && string.IsNullOrWhiteSpace(configuration.SmtpHost))
            ? services.AddSingleton<IEmailSender, LoggingEmailSender>()
            : services.AddSingleton<IEmailSender, SmtpEmailSender>();

    public static IServiceCollection AddChatModel(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        configuration.AiChatProvider switch
        {
            AiConstants.ProviderOpenAi => services.AddOpenAiChatModel(configuration),
            var provider => throw new InvalidOperationException($"Unsupported AI provider '{provider}'."),
        };

    private static IServiceCollection AddOpenAiChatModel(this IServiceCollection services, EnvironmentConfiguration configuration)
    {
        services.AddHttpClient<IChatModelClient, OpenAiChatModelClient>(client =>
        {
            client.BaseAddress = new Uri($"{configuration.AiChatBaseUrl.AbsoluteUri.TrimEnd('/')}/");
            client.Timeout = Timeout.InfiniteTimeSpan;
            if (configuration.AiChatConfigured)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.AiChatApiKey);
            }
        });
        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IHealthRepository, HealthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<ITodoEventRepository, TodoEventRepository>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITodoService, TodoService>();
        services.AddScoped<IAiUsageService, AiUsageService>();
        services.AddScoped<IAssistantService, AssistantService>();
        services.AddScoped<IAssistantTools, AssistantTools>();
        services.AddSingleton<ITokenService, TokenService>();
        return services;
    }
}
