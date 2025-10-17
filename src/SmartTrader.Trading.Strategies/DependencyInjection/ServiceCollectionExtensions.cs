using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SmartTrader.Trading.Strategies.Loading;
using SmartTrader.Trading.Strategies.Options;
using SmartTrader.Trading.Strategies.Runtime;

namespace SmartTrader.Trading.Strategies.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStrategyPlugins(this IServiceCollection services, Action<StrategyPluginOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<StrategyPluginOptions>, StrategyPluginOptionsValidator>());
        services.TryAddSingleton<IStrategyPluginLoader, StrategyPluginLoader>();
        services.TryAddSingleton<IStrategyCatalog, StrategyCatalog>();

        return services;
    }
}
