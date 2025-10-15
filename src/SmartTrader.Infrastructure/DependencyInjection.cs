using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using SmartTrader.Infrastructure.Persistence;
using SmartTrader.Infrastructure.Persistence.Repositories;

namespace SmartTrader.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Single database with multiple schemas (core, market)
        var connectionString = configuration.GetConnectionString("CoreDatabase")
                                ?? configuration.GetConnectionString("MarketDatabase");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Redis (shared multiplexer)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

        return services;
    }
}


