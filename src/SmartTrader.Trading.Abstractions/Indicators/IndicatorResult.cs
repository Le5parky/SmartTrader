using System.Collections.ObjectModel;
using System.Text.Json;

namespace SmartTrader.Trading.Abstractions.Indicators;

public sealed class IndicatorResult
{
    public const string PrimaryValueKey = "value";

    public IndicatorResult(
        string name,
        string symbol,
        string timeframe,
        DateTimeOffset candleTimestamp,
        IReadOnlyDictionary<string, decimal> values)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Indicator name must be provided.", nameof(name))
            : name;
        Symbol = string.IsNullOrWhiteSpace(symbol)
            ? throw new ArgumentException("Symbol must be provided.", nameof(symbol))
            : symbol;
        Timeframe = string.IsNullOrWhiteSpace(timeframe)
            ? throw new ArgumentException("Timeframe must be provided.", nameof(timeframe))
            : timeframe;
        CandleTimestamp = candleTimestamp;
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        Values = new ReadOnlyDictionary<string, decimal>(
            new Dictionary<string, decimal>(values, StringComparer.OrdinalIgnoreCase));
    }

    public string Name { get; }
    public string Symbol { get; }
    public string Timeframe { get; }
    public DateTimeOffset CandleTimestamp { get; }
    public IReadOnlyDictionary<string, decimal> Values { get; }

    public decimal Value => Values.TryGetValue(PrimaryValueKey, out var v)
        ? v
        : throw new InvalidOperationException($"Indicator {Name} does not expose a value under key '{PrimaryValueKey}'.");

    public bool TryGetValue(string key, out decimal value) => Values.TryGetValue(key, out value);

    public JsonDocument ToJson()
    {
        var buffer = new Dictionary<string, decimal>(Values, StringComparer.OrdinalIgnoreCase);
        return JsonDocument.Parse(JsonSerializer.Serialize(buffer));
    }

    public static IndicatorResult FromSingleValue(IndicatorRequest request, decimal value)
    {
        var dict = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [PrimaryValueKey] = value
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, dict);
    }
}
