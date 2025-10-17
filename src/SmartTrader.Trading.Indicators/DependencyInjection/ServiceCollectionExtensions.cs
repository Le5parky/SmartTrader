using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Indicators.Caching;
using SmartTrader.Trading.Indicators.Calculators;
using SmartTrader.Trading.Indicators.Options;
using StackExchange.Redis;

namespace SmartTrader.Trading.Indicators.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingIndicators(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddOptions<IndicatorCacheOptions>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, SmaCalculator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, EmaCalculator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, RsiCalculator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, BollingerBandsCalculator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, AtrCalculator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIndicatorCalculator, AdxCalculator>());

        services.TryAddSingleton<IIndicatorCache>(sp =>
        {
            var memory = sp.GetRequiredService<IMemoryCache>();
            var options = sp.GetRequiredService<IOptions<IndicatorCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<IndicatorCache>>();
            var redis = sp.GetService<IConnectionMultiplexer>();
            var database = redis?.GetDatabase();
            return new IndicatorCache(memory, options, logger, database);
        });

        services.TryAddSingleton<IIndicatorProvider, IndicatorProvider>();

        return services;
    }
}
