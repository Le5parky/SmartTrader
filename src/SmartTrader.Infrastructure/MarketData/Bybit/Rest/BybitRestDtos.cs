using System.Text.Json.Serialization;

namespace SmartTrader.Infrastructure.MarketData.Bybit.Rest;

internal sealed class BybitKlineResponse
{
    [JsonPropertyName("retCode")]
    public int RetCode { get; set; }

    [JsonPropertyName("retMsg")]
    public string? RetMsg { get; set; }

    [JsonPropertyName("result")]
    public BybitKlineResult? Result { get; set; }

    [JsonPropertyName("retExtInfo")]
    public object? RetExtInfo { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }
}

internal sealed class BybitKlineResult
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("list")]
    public List<BybitKlineItem> Items { get; set; } = new();
}

internal sealed class BybitKlineItem
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("open")]
    public string? Open { get; set; }

    [JsonPropertyName("high")]
    public string? High { get; set; }

    [JsonPropertyName("low")]
    public string? Low { get; set; }

    [JsonPropertyName("close")]
    public string? Close { get; set; }

    [JsonPropertyName("volume")]
    public string? Volume { get; set; }

    [JsonPropertyName("confirm")]
    public string? Confirm { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
