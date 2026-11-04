using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Infrastructure;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, EnvironmentConfiguration configuration)
    {
        services.AddDataProtection();
        services.AddIdentityCore<User>(options => ConfigureIdentity(options, configuration))
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

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

    private static void ConfigureIdentity(IdentityOptions options, EnvironmentConfiguration configuration)
    {
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = string.Empty;
        options.Password.RequiredLength = AuthConstants.PasswordMinLength;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = configuration.LockoutMaxFailures;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(configuration.LockoutMinutes);
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
