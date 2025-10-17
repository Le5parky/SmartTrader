using SmartTrader.Domain.MarketData;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public interface ICandleWriteRepository
{
    Task UpsertAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken);
}
