using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.Persistence;
using SmartTrader.Infrastructure.Persistence.Entities;
using SmartTrader.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;

namespace SmartTrader.IntegrationTests;

public class CandleWriteRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public CandleWriteRepositoryTests()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("smarttrader_test")
            .Build();
    }

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Upsert_IsIdempotent()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString());

        await using (var setupContext = new AppDbContext(optionsBuilder.Options))
        {
            await setupContext.Database.MigrateAsync();
        }

        var factory = new PooledDbContextFactory<AppDbContext>(optionsBuilder.Options);
        var repository = new CandleWriteRepository(factory, NullLogger<CandleWriteRepository>.Instance);

        var candle = new Candle(DateTimeOffset.UtcNow, 1m, 2m, 0.5m, 1.5m, 42m);

        await repository.UpsertAsync("BTCUSDT", Timeframe.M1, new[] { candle }, CancellationToken.None);
        await repository.UpsertAsync("BTCUSDT", Timeframe.M1, new[] { candle }, CancellationToken.None);

        await using var verifyContext = await factory.CreateDbContextAsync();
        var count = await verifyContext.Set<Candle1m>().CountAsync();

        Assert.Equal(1, count);
    }
}


