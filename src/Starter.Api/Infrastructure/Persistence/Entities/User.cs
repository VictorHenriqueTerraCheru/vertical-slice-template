using Microsoft.AspNetCore.Identity;
using Starter.Api.Common;

namespace Starter.Api.Persistence;

public sealed class User : IdentityUser<Guid>
{
    public required string Name { get; set; }

    public UserRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
