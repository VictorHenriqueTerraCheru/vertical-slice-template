using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Testcontainers.PostgreSql;

namespace Starter.Api.Tests.Infra;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public CapturingEmailSender Emails { get; } = new();

    public FakeTimeProvider Clock { get; } = new(PostgresClock.Truncate(DateTimeOffset.UtcNow));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentNames.Testing);
        builder.UseSetting(ConfigurationKeys.DatabaseConnection, _database.GetConnectionString());
        builder.UseSetting(ConfigurationKeys.JwtKey, "test-key-with-at-least-32-characters-long");
        builder.UseSetting(ConfigurationKeys.JwtIssuer, "starter-tests");
        builder.UseSetting(ConfigurationKeys.JwtAudience, "starter-tests");
        builder.UseSetting(ConfigurationKeys.RateLimitAuthPerMinute, "100000");
        builder.ConfigureTestServices(services => services
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton<IEmailSender>(Emails));
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await InDatabaseAsync(db => db.Database.MigrateAsync());
    }

    public async Task<T> InDatabaseAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public Task InDatabaseAsync(Func<AppDbContext, Task> action) =>
        InDatabaseAsync(async db =>
        {
            await action(db);
            return true;
        });

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }
}
