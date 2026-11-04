namespace Starter.Api.Users;

public sealed record PasswordResetGrant(string Email, string Token);
