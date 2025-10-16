using SmartTrader.Domain.MarketData;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public interface ICandleWriteRepository
{
    Task UpsertAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken);
}
