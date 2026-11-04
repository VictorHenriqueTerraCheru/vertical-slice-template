using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Tests.Infra;

[Collection(nameof(ApiCollection))]
public sealed class ProductionStartupTests(ApiFactory factory)
{
    private static EnvironmentConfiguration ProductionConfiguration(string? adminEmail, string? adminPassword) =>
        new(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_CONNECTION"] = "Host=localhost;Database=x",
                ["JWT_KEY"] = "a-production-key-with-more-than-32-characters",
                ["JWT_ISSUER"] = "issuer",
                ["JWT_AUDIENCE"] = "audience",
                ["SMTP_HOST"] = "smtp.example.com",
                ["SMTP_PORT"] = "587",
                ["SMTP_USER"] = "user",
                ["SMTP_PASSWORD"] = "password",
                ["SMTP_SENDER_NAME"] = "Starter",
                ["SMTP_SENDER_EMAIL"] = "no-reply@example.com",
                ["PASSWORD_RESET_URL"] = "https://app.example.com/reset",
                ["CORS_ORIGINS"] = "https://app.example.com",
                ["INITIAL_ADMIN_EMAIL"] = adminEmail,
                ["INITIAL_ADMIN_PASSWORD"] = adminPassword,
            }).Build(),
            EnvironmentNames.Production);

    private Task RunAsync(string? adminEmail, string? adminPassword) =>
        ProductionStartup.RunAsync(factory.Services, ProductionConfiguration(adminEmail, adminPassword), CancellationToken.None);

    private Task<User?> FindAsync(string email) =>
        factory.InDatabaseAsync(db => db.Set<User>().SingleOrDefaultAsync(user => user.Email == email));

    [Fact]
    public async Task Production_creates_the_initial_admin_and_no_sample_users()
    {
        var email = Scenarios.NewEmail();

        await RunAsync(email, Scenarios.Password);

        var admin = await FindAsync(email);
        admin.Should().NotBeNull();
        admin!.Role.Should().Be(UserRole.Administrator);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        (await users.CheckPasswordAsync(admin, Scenarios.Password)).Should().BeTrue();
        (await FindAsync(SeedAccounts.AdministratorEmail)).Should().BeNull();
        (await FindAsync(SeedAccounts.UserEmail)).Should().BeNull();
    }

    [Fact]
    public async Task Production_promotes_an_existing_account_with_the_admin_email()
    {
        var existing = await Scenarios.RegisterAsync(factory);

        await RunAsync(existing.Email, "a-different-password-123");

        var promoted = await FindAsync(existing.Email);
        promoted!.Role.Should().Be(UserRole.Administrator);
        promoted.Id.Should().Be(existing.UserId);
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        (await users.CheckPasswordAsync(promoted, existing.Password)).Should().BeTrue();
    }

    [Fact]
    public async Task Production_without_admin_variables_creates_nobody()
    {
        var before = await factory.InDatabaseAsync(db => db.Set<User>().CountAsync());

        await RunAsync(null, null);

        (await factory.InDatabaseAsync(db => db.Set<User>().CountAsync())).Should().Be(before);
    }
}
