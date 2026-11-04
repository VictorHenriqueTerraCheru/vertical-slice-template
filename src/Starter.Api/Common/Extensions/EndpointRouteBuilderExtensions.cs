using System.Reflection;

namespace Starter.Api.Common;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup(string.Empty).RequireAuthorization(AuthorizationPolicies.Authenticated);

        foreach (var module in Modules())
        {
            module.GetMethod(nameof(IEndpointModule.Map), BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [endpoints]);
        }

        return app;
    }

    private static IEnumerable<Type> Modules() =>
        typeof(EndpointRouteBuilderExtensions).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IEndpointModule).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal);
}
