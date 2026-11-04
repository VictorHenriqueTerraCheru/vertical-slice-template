using Starter.Api.Common;
using Starter.Api.Infrastructure;
using Starter.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);

EnvironmentConfiguration configuration;
try
{
    configuration = builder.AddEnvironmentConfiguration();
}
catch (InvalidOperationException exception)
{
    await Console.Error.WriteLineAsync(exception.Message);
    return 1;
}

builder.Services
    .AddApplication(configuration)
    .AddProxy(configuration)
    .AddCorsPolicy(configuration)
    .AddRateLimiting(configuration)
    .AddOpenApiDocumentation()
    .AddAuthenticationAndAuthorization(configuration);

var app = builder.Build();

app.UseProxy();
app.UseErrorHandling();
app.UseSecurityHeaders();
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiting();

if (configuration.Environment != EnvironmentNames.Production)
{
    app.MapOpenApiDocumentation();
}

app.MapEndpoints();

switch (configuration.Environment)
{
    case EnvironmentNames.Development:
        await DevelopmentSeed.RunAsync(app.Services, app.Lifetime.ApplicationStopping);
        break;
    case EnvironmentNames.Staging or EnvironmentNames.Production:
        await ProductionStartup.RunAsync(app.Services, configuration, app.Lifetime.ApplicationStopping);
        break;
}

await app.RunAsync();
return 0;

public partial class Program;
