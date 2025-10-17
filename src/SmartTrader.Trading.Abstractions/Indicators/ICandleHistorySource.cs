using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Abstractions.Indicators;

public interface ICandleHistorySource
{
    /// <summary>
    /// Returns candles ordered ascending by open timestamp, ending at or before the specified candle timestamp.
    /// </summary>
    Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        string timeframe,
        DateTimeOffset uptoInclusive,
        int lookback,
        CancellationToken cancellationToken);
}
