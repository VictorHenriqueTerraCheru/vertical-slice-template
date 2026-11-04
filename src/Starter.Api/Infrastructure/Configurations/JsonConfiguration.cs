using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class JsonConfiguration
{
    public static IServiceCollection AddJson(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(options => JsonConfig.Configure(options.SerializerOptions));
}
