using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public sealed record UserResponse(Guid Id, string Name, string Email, UserRole Role)
{
    public static UserResponse From(User user) => new(user.Id, user.Name, user.Email!, user.Role);
}
