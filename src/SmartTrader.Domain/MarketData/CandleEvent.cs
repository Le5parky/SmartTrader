namespace SmartTrader.Domain.MarketData;

public sealed record CandleEvent(Candle Candle, bool IsClosed);
