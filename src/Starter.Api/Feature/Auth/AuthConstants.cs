namespace Starter.Api.Auth;

public static class AuthConstants
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int RefreshTokenBytes = 32;
    public const int TokenHashLength = 64;
    public const int RefreshTokenMaxLength = 128;
    public const int PasswordResetTokenMaxLength = 1024;
    public const string DefaultPasswordResetUrl = "http://localhost:3000/reset-password";
    public const string PasswordResetSubject = "Redefinição de senha";
}
