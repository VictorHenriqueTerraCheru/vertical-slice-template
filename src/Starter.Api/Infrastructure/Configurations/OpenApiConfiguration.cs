using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Starter.Api.Infrastructure;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = info =>
                OpenApiOptions.CreateDefaultSchemaReferenceId(info) is null ? null : SchemaName(info.Type);

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                };
                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
                {
                    if (parameter.In == ParameterLocation.Query && parameter.Name is { Length: > 0 } name)
                    {
                        parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
                    }
                }

                var anonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();
                if (!anonymous)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [] },
                    ];
                }

                return Task.CompletedTask;
            });
        });

    public static WebApplication MapOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
        return app;
    }

    public static string SchemaName(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        var name = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
        if (type.IsNested && type.DeclaringType is { } external && !type.IsGenericParameter)
        {
            name = external.Name + name;
        }

        return type.IsGenericType
            ? name + string.Concat(type.GetGenericArguments().Select(SchemaName))
            : name;
    }
}
