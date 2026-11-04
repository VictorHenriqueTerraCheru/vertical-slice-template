using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public sealed class AdministratorPolicyTests(ApiFactory factory)
{
    private async Task<bool> SatisfiesAdministratorPolicyAsync(UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = Scenarios.NewEmail(),
            Name = "Policy User",
            Role = role,
            CreatedAt = factory.Clock.GetUtcNow(),
        };
        var token = factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user);
        var claims = new JsonWebTokenHandler().ReadJsonWebToken(token.Value).Claims;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimNames.Name, ClaimNames.Role));

        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(principal, AuthorizationPolicies.Administrator);
        return result.Succeeded;
    }

    [Fact]
    public async Task Administrator_token_satisfies_the_administrator_policy()
    {
        (await SatisfiesAdministratorPolicyAsync(UserRole.Administrator)).Should().BeTrue();
    }

    [Fact]
    public async Task Regular_user_token_does_not_satisfy_the_administrator_policy()
    {
        (await SatisfiesAdministratorPolicyAsync(UserRole.User)).Should().BeFalse();
    }
}
