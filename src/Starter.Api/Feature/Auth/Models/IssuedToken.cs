namespace Starter.Api.Auth;

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);
