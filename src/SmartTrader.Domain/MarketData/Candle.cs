namespace SmartTrader.Domain.MarketData;

public sealed record Candle(
    DateTimeOffset TsOpenUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
