using Starter.Api.Persistence;

namespace Starter.Api.Auth;

public interface ITokenService
{
    IssuedToken CreateAccessToken(User user);

    IssuedToken CreateRefreshToken();

    string Hash(string refreshToken);
}
