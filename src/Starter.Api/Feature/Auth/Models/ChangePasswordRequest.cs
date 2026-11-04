namespace Starter.Api.Auth;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
