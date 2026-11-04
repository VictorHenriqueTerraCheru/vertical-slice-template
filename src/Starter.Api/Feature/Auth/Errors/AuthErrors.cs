using Starter.Api.Common;

namespace Starter.Api.Auth;

public static class AuthErrors
{
    public static ApplicationError InvalidCredentials { get; } = ApplicationError.Rule(
        StatusCodes.Status401Unauthorized, null, "E-mail ou senha inválidos.");
}
