using System.Globalization;
using System.Text.Json;
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
    [JsonConverter(typeof(BybitFlexibleLongConverter))]
    public long? Start { get; set; }

    [JsonPropertyName("end")]
    [JsonConverter(typeof(BybitFlexibleLongConverter))]
    public long? End { get; set; }

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
    [JsonConverter(typeof(BybitFlexibleBoolConverter))]
    public bool? Confirm { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(BybitFlexibleLongConverter))]
    public long? Timestamp { get; set; }
}

internal sealed class BybitFlexibleLongConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var number) => number,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}

internal sealed class BybitFlexibleBoolConverter : JsonConverter<bool?>
{
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => ParseString(reader.GetString()),
            JsonTokenType.Number => reader.TryGetInt64(out var number) ? number != 0 : (bool?)null,
            _ => null
        };
    }

    private static bool? ParseString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (bool.TryParse(value, out var boolean))
        {
            return boolean;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return number != 0;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteBooleanValue(value.Value);
        }
    }
}
