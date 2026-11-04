namespace Starter.Api.Infrastructure;

public static class ErrorHandlingConfiguration
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services) =>
        services.AddProblemDetails();

    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
