namespace SmartTrader.Infrastructure.Persistence.Entities;

public class Symbol
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string BaseAsset { get; set; } = null!;
    public string QuoteAsset { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class Candle1m
{
    public Guid SymbolId { get; set; }
    public DateTimeOffset TsOpen { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

public class IndicatorsCache
{
    public Guid SymbolId { get; set; }
    public string Timeframe { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTimeOffset CandleTs { get; set; }
    public string Values { get; set; } = null!;
}


