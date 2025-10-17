namespace SmartTrader.Trading.Abstractions.Indicators;

public interface IIndicatorCache
{
    Task<IndicatorResult?> TryGetAsync(IndicatorRequest request, CancellationToken cancellationToken);

    Task SetAsync(IndicatorRequest request, IndicatorResult result, TimeSpan ttl, CancellationToken cancellationToken);

    Task InvalidateAsync(string symbol, string timeframe, DateTimeOffset candleTimestamp, CancellationToken cancellationToken);
}
