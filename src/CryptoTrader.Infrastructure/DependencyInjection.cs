using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using CryptoTrader.Infrastructure.Persistence;
using CryptoTrader.Infrastructure.Persistence.Repositories;
using CryptoTrader.Domain.Interfaces;
using CryptoTrader.Infrastructure.ByBit;

namespace CryptoTrader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddPooledDbContextFactory<TradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDbContext<TradingDbContext>(options =>
            options.UseNpgsql(connectionString));

        var useDemo = configuration.GetValue<bool>("ByBit:useDemo");
        var apiKey = useDemo ? configuration["ByBit:apiKeyDemo"] : configuration["ByBit:apiKey"];
        var apiSecret = useDemo ? configuration["ByBit:apiSecretDemo"] : configuration["ByBit:apiSecret"];

        services.AddByBit(useDemo, apiKey ?? "", apiSecret ?? "");

        // Redis (shared multiplexer)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddSingleton<IPositionStateRepository, PositionStateRepository>();

        return services;
    }
}
