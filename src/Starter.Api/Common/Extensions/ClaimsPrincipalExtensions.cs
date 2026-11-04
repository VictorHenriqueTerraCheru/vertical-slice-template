using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;

namespace Starter.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Token without the 'sub' claim."));

    public static Guid? GetUserIdOrNull(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
