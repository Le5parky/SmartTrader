namespace SmartTrader.Trading.Indicators.Options;

public sealed class IndicatorCacheOptions
{
    public TimeSpan MemoryTtl { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan RedisTtl { get; set; } = TimeSpan.FromHours(1);

    public string KeyPrefix { get; set; } = "ind";

    public string IndexPrefix { get; set; } = "ind:index";
}
