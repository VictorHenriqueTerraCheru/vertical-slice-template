using Starter.Api.Common;
using Starter.Api.Infrastructure;

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
    .AddOpenApiDocumentation()
    .AddAuthenticationAndAuthorization();

var app = builder.Build();

app.UseErrorHandling();
app.UseAuthentication();
app.UseAuthorization();

if (configuration.Environment != EnvironmentNames.Production)
{
    app.MapOpenApiDocumentation();
}

app.MapEndpoints();

await app.RunAsync();
return 0;

public partial class Program;
