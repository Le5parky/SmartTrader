namespace SmartTrader.Infrastructure.MarketData.Bybit.Options;

public sealed class BybitOptions
{
    public const string SectionName = "Bybit";

    public string BaseUrl { get; set; } = "https://api.bybit.com";
    public string WsUrl { get; set; } = "wss://stream.bybit.com/v5/public/linear";
    public string Category { get; set; } = "linear";
    public string[] Symbols { get; set; } = Array.Empty<string>();
    public string[] Timeframes { get; set; } = Array.Empty<string>();
    public BybitRestOptions Rest { get; set; } = new();
    public BybitWsOptions Ws { get; set; } = new();
    public BybitRateLimitOptions RateLimit { get; set; } = new();
}

public sealed class BybitRestOptions
{
    public int TimeoutSec { get; set; } = 10;
    public int PageSize { get; set; } = 1000;
    public int BackfillDays { get; set; } = 90;
    public int MaxConcurrency { get; set; } = 2;
}

public sealed class BybitWsOptions
{
    public int ReconnectBaseMs { get; set; } = 500;
    public int ReconnectMaxMs { get; set; } = 10_000;
    public int HeartbeatMs { get; set; } = 20_000;
}

public sealed class BybitRateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstSize { get; set; } = 10;
}
