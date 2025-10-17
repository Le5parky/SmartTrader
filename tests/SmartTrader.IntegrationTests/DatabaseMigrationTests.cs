using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using SmartTrader.Infrastructure.Persistence;
using Xunit;
using SmartTrader.IntegrationTests.TestInfrastructure;
using Testcontainers.PostgreSql;

namespace SmartTrader.IntegrationTests;

public class DatabaseMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    public DatabaseMigrationTests()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("smarttrader_core")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [RequiresDockerFact]
    public async Task ApplyMigrations_Succeeds()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var ctx = new AppDbContext(options);
        await ctx.Database.MigrateAsync();

        // Basic smoke check: ensure at least one table exists by querying for pending migrations == 0
        var pending = await ctx.Database.GetPendingMigrationsAsync();
        Assert.Empty(pending);
    }
}




