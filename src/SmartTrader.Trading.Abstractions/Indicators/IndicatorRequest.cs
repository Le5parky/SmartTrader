using System.Collections.ObjectModel;

namespace SmartTrader.Trading.Abstractions.Indicators;

public sealed class IndicatorRequest
{
    public IndicatorRequest(
        string name,
        string symbol,
        string timeframe,
        DateTimeOffset candleTimestamp,
        IReadOnlyDictionary<string, decimal>? arguments = null)
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
        Arguments = arguments is null
            ? new ReadOnlyDictionary<string, decimal>(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase))
            : new ReadOnlyDictionary<string, decimal>(
                new Dictionary<string, decimal>(arguments, StringComparer.OrdinalIgnoreCase));
    }

    public string Name { get; }
    public string Symbol { get; }
    public string Timeframe { get; }
    public DateTimeOffset CandleTimestamp { get; }
    public IReadOnlyDictionary<string, decimal> Arguments { get; }

    public bool TryGetArgument(string name, out decimal value) =>
        Arguments.TryGetValue(name, out value);

    public int GetRequiredInt(string name)
    {
        if (!Arguments.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"Argument '{name}' is required for indicator '{Name}'.");
        }

        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public decimal GetRequiredDecimal(string name)
    {
        if (!Arguments.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"Argument '{name}' is required for indicator '{Name}'.");
        }

        return value;
    }

    public int GetOptionalInt(string name, int fallback)
    {
        return Arguments.TryGetValue(name, out var value)
            ? (int)Math.Round(value, MidpointRounding.AwayFromZero)
            : fallback;
    }

    public decimal GetOptionalDecimal(string name, decimal fallback)
    {
        return Arguments.TryGetValue(name, out var value) ? value : fallback;
    }
}
