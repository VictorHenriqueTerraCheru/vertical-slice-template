using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Starter.Api.Common;

namespace Starter.Api.Persistence;

public static class DevelopmentSeed
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        if (await users.FindByEmailAsync(SeedAccounts.AdministratorEmail) is null)
        {
            await AccountSeeder.CreateAsync(
                users, clock, SeedAccounts.AdministratorEmail, SeedAccounts.AdministratorName, UserRole.Administrator, SeedAccounts.Password);
        }

        if (await users.FindByEmailAsync(SeedAccounts.UserEmail) is null)
        {
            await AccountSeeder.CreateAsync(
                users, clock, SeedAccounts.UserEmail, SeedAccounts.UserName, UserRole.User, SeedAccounts.Password);
        }
    }
}
