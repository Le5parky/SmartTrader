using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SmartTrader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL DbContexts
        var coreConnection = configuration.GetConnectionString("CoreDatabase");
        var marketConnection = configuration.GetConnectionString("MarketDatabase");

        services.AddDbContext<Persistence.CoreDbContext>(options =>
            options.UseNpgsql(coreConnection));

        services.AddDbContext<Persistence.MarketDbContext>(options =>
            options.UseNpgsql(marketConnection));

        // Redis (shared multiplexer)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        return services;
    }
}


