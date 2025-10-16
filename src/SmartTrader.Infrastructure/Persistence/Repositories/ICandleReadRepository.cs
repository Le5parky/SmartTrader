using SmartTrader.Domain.MarketData;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public interface ICandleReadRepository
{
    Task<DateTimeOffset?> GetLastCandleOpenAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken);
}
