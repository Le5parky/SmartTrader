using SmartTrader.Domain.MarketData;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public interface ICandleReadRepository
{
    Task<DateTimeOffset?> GetLastCandleOpenAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken);

    Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset uptoInclusive,
        int lookback,
        CancellationToken cancellationToken);
}
