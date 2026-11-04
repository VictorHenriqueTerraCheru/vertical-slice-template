using Starter.Api.Common;

namespace Starter.Api.Users;

public static class UserErrors
{
    public static ApplicationError EmailAlreadyInUse { get; } = ApplicationError.Rule(
        StatusCodes.Status409Conflict, "email-already-in-use", "Já existe uma conta com este e-mail.");

    public static ApplicationError IdentityFailure { get; } = ApplicationError.Rule(
        StatusCodes.Status500InternalServerError, "identity-failure", "Não foi possível concluir a operação. Tente novamente.");

    public static ApplicationError InvalidPasswordResetToken { get; } = ApplicationError.Rule(
        StatusCodes.Status400BadRequest, "invalid-password-reset-token", "O link de redefinição é inválido ou expirou.");

    public static ApplicationError WrongCurrentPassword { get; } = ApplicationError.FromField(
        "currentPassword", "A senha atual está incorreta.");
}
