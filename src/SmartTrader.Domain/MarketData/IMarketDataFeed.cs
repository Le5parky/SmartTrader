namespace SmartTrader.Domain.MarketData;

public interface IMarketDataFeed
{
    Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);

    IAsyncEnumerable<CandleEvent> StreamKlinesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken);
}
