using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<EnvironmentConfiguration, TimeProvider>(ConfigureBearer);

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AuthorizationPolicies.Administrator, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(ClaimNames.Role, RoleNames.Administrator));

        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, EnvironmentConfiguration configuration, TimeProvider clock)
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = configuration.JwtIssuer,
            ValidAudience = configuration.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration.JwtKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ClaimNames.Name,
            RoleClaimType = ClaimNames.Role,
            LifetimeValidator = (start, end, _, parameters) =>
            {
                var now = clock.GetUtcNow().UtcDateTime;
                var slack = parameters.ClockSkew;
                return (start is null || start.Value <= now + slack)
                    && end is not null && end.Value > now - slack;
            },
        };
    }
}
