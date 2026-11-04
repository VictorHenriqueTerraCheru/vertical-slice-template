using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Starter.Api.Common;

namespace Starter.Api.Persistence;

public static class ProductionStartup
{
    public static async Task RunAsync(
        IServiceProvider services, EnvironmentConfiguration configuration, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(configuration.InitialAdminEmail) || string.IsNullOrWhiteSpace(configuration.InitialAdminPassword))
        {
            return;
        }

        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var existing = await users.FindByEmailAsync(configuration.InitialAdminEmail);
        if (existing is null)
        {
            await AccountSeeder.CreateAsync(
                users,
                scope.ServiceProvider.GetRequiredService<TimeProvider>(),
                configuration.InitialAdminEmail,
                SeedAccounts.InitialAdministratorName,
                UserRole.Administrator,
                configuration.InitialAdminPassword);
            return;
        }

        if (existing.Role != UserRole.Administrator)
        {
            existing.Role = UserRole.Administrator;
            var result = await users.UpdateAsync(existing);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not promote the account {configuration.InitialAdminEmail}: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
            }
        }
    }
}
