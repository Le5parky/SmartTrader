using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.Persistence.Repositories;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Infrastructure.MarketData;

public sealed class CandleHistorySource : ICandleHistorySource
{
    private readonly ICandleReadRepository _repository;

    public CandleHistorySource(ICandleReadRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        string timeframe,
        DateTimeOffset uptoInclusive,
        int lookback,
        CancellationToken cancellationToken)
    {
        if (!TimeframeExtensions.TryParse(timeframe, out var parsed) || parsed is null)
        {
            throw new ArgumentException($"Unsupported timeframe '{timeframe}'.", nameof(timeframe));
        }

        return _repository.GetHistoryAsync(symbol, parsed.Value, uptoInclusive, lookback, cancellationToken);
    }
}
