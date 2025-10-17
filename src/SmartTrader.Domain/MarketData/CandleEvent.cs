using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Domain.MarketData;

public sealed record CandleEvent(Candle Candle, bool IsClosed);
