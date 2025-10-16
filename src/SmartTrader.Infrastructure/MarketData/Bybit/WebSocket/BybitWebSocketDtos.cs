using System.Text.Json.Serialization;

namespace SmartTrader.Infrastructure.MarketData.Bybit.WebSocket;

internal sealed class BybitWsEnvelope
{
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    [JsonPropertyName("op")]
    public string? Op { get; set; }

    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    [JsonPropertyName("ret_msg")]
    public string? RetMsg { get; set; }

    [JsonPropertyName("conn_id")]
    public string? ConnectionId { get; set; }

    [JsonPropertyName("data")]
    public List<BybitWsKline>? Data { get; set; }

    [JsonPropertyName("req_id")]
    public string? RequestId { get; set; }
}

internal sealed class BybitWsKline
{
    [JsonPropertyName("start")]
    public string? Start { get; set; }

    [JsonPropertyName("end")]
    public string? End { get; set; }

    [JsonPropertyName("interval")]
    public string? Interval { get; set; }

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
