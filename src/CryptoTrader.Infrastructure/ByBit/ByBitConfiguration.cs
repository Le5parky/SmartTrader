using Microsoft.Extensions.DependencyInjection;
using Bybit.Net;
using Bybit.Net.Objects;

namespace CryptoTrader.Infrastructure.ByBit;

public static class ByBitConfiguration
{
    public static IServiceCollection AddByBit(this IServiceCollection services, bool useDemo, string apiKey, string apiSecret)
    {
        services.AddBybit(options =>
        {
            options.Environment = useDemo
                ? BybitEnvironment.DemoTrading
                : BybitEnvironment.Live;
            options.ApiCredentials = new BybitCredentials(apiKey, apiSecret);
        });

        return services;
    }
}
