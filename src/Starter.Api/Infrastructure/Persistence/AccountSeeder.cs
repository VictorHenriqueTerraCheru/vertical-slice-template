using Microsoft.AspNetCore.Identity;

using Starter.Api.Common;

namespace Starter.Api.Persistence;

public static class AccountSeeder
{
    public static async Task CreateAsync(
        UserManager<User> users, TimeProvider clock, string email, string name, UserRole role, string password)
    {
        var now = clock.GetUtcNow();
        var user = new User
        {
            Id = Guid.CreateVersion7(now),
            Name = name,
            Email = email,
            UserName = email,
            Role = role,
            CreatedAt = now,
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the account {email}: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
    }
}
