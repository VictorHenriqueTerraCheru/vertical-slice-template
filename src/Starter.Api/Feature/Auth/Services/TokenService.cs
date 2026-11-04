using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Auth;

public sealed class TokenService(EnvironmentConfiguration configuration, TimeProvider clock) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.JwtKey)), SecurityAlgorithms.HmacSha256);

    public IssuedToken CreateAccessToken(User user)
    {
        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(configuration.AccessTokenMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = configuration.JwtIssuer,
            Audience = configuration.JwtAudience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email!,
                [ClaimNames.Name] = user.Name,
                [ClaimNames.Role] = RoleName(user.Role),
            },
        });

        return new IssuedToken(token, expiresAt);
    }

    public IssuedToken CreateRefreshToken()
    {
        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(AuthConstants.RefreshTokenBytes));
        return new IssuedToken(value, clock.GetUtcNow().AddDays(configuration.RefreshTokenDays));
    }

    private static string RoleName(UserRole role) => role switch
    {
        UserRole.User => RoleNames.User,
        UserRole.Administrator => RoleNames.Administrator,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    public string Hash(string refreshToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
