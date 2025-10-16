using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit;
using SmartTrader.Infrastructure.MarketData.Bybit.Internal;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;
using SmartTrader.Infrastructure.MarketData.Bybit.Rest;
using SmartTrader.Infrastructure.MarketData.Bybit.WebSocket;
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
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Redis (shared multiplexer)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ICandleWriteRepository, CandleWriteRepository>();
        services.AddScoped<ICandleReadRepository, CandleReadRepository>();

services.AddSingleton<IValidateOptions<BybitOptions>, BybitOptionsValidator>();
        services.AddOptions<BybitOptions>()
            .Configure(options => configuration.GetSection(BybitOptions.SectionName).Bind(options))
            .ValidateOnStart();

        services.AddSingleton<IBybitRateLimiter>(sp =>
        {
            var multiplexer = sp.GetService<IConnectionMultiplexer>();
            return new BybitRateLimiter(multiplexer, sp.GetRequiredService<IOptions<BybitOptions>>());
        });

        services.AddSingleton<IBybitWebSocketClient, BybitWebSocketClient>();

        services.AddHttpClient<IBybitRestClient, BybitRestClient>((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<BybitOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(opts.Rest.TimeoutSec);
            })
            .AddPolicyHandler((sp, _) =>
            {
                var logger = sp.GetRequiredService<ILogger<BybitRestClient>>();
                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TaskCanceledException>()
                    .WaitAndRetryAsync(
                        retryCount: 5,
                        sleepDurationProvider: attempt =>
                        {
                            var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 750));
                            return backoff + jitter;
                        },
                        onRetry: (outcome, span, attempt, _) =>
                        {
                            logger.LogWarning(
                                outcome.Exception,
                                "Retrying Bybit REST request (attempt {Attempt}) after {Delay}.",
                                attempt,
                                span);
                        });
            })
            .AddPolicyHandler((sp, _) =>
            {
                var opts = sp.GetRequiredService<IOptions<BybitOptions>>().Value;
                return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(opts.Rest.TimeoutSec));
            });

        services.AddSingleton<IMarketDataFeed, BybitMarketDataFeed>();

        return services;
    }
}
















