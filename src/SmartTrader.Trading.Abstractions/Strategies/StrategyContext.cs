using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Abstractions.Strategies;

public sealed class StrategyContext
{
    private IReadOnlyDictionary<string, JsonElement>? _paramsCache;

    public required string Symbol { get; init; }
    public required string Timeframe { get; init; }
    public required DateTimeOffset CandleTimestamp { get; init; }
    public required IReadOnlyList<Candle> History { get; init; }
    public required JsonDocument Parameters { get; init; }
    public required IIndicatorProvider Indicators { get; init; }

    public T GetParameter<T>(string name)
    {
        _paramsCache ??= new ReadOnlyDictionary<string, JsonElement>(
            Parameters.RootElement
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase));

        if (!_paramsCache.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"Parameter '{name}' is not defined for strategy context {Symbol}/{Timeframe}.");
        }

        return value.Deserialize<T>() ?? throw new InvalidOperationException($"Parameter '{name}' cannot be converted to {typeof(T).Name}.");
    }
}
