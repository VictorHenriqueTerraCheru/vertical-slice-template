using Starter.Api.Auth;

namespace Starter.Api.Tests.Infra;

public sealed record RegisteredUser(string Email, string Password, TokenPair Tokens, ApiClient Client, Guid UserId);
