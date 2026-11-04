namespace Starter.Api.Auth;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
