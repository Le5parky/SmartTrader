namespace SmartTrader.Trading.Abstractions.Models;

public sealed record Candle(
    DateTimeOffset TsOpenUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
