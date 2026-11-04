using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class EnvironmentConfigurationSetup
{
    public static EnvironmentConfiguration AddEnvironmentConfiguration(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        if (environment == EnvironmentNames.Development)
        {
            builder.Configuration.AddDotEnvFile(builder.Environment.ContentRootPath);
        }

        var configuration = new EnvironmentConfiguration(builder.Configuration, environment);
        builder.Services.AddSingleton(configuration);
        return configuration;
    }
}
