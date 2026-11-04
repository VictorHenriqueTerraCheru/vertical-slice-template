using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Testcontainers.PostgreSql;

namespace Starter.Api.Tests.Infra;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public FakeTimeProvider Clock { get; } = new(PostgresClock.Truncate(DateTimeOffset.UtcNow));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("DB_CONNECTION", _database.GetConnectionString());
        builder.UseSetting("JWT_KEY", "test-key-with-at-least-32-characters-long");
        builder.UseSetting("JWT_ISSUER", "starter-tests");
        builder.UseSetting("JWT_AUDIENCE", "starter-tests");
        builder.UseSetting("RATE_LIMIT_AUTH_PER_MINUTE", "100000");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
    }

    public async Task InitializeAsync() => await _database.StartAsync();

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
