using System.Globalization;
using System.Linq;
using System.Text;
using SmartTrader.Trading.Abstractions.Indicators;

namespace SmartTrader.Trading.Indicators.Caching;

internal readonly record struct IndicatorCacheKey(string Value, string BaseKey)
{
    public static IndicatorCacheKey From(IndicatorRequest request)
    {
        var name = request.Name.Trim().ToLowerInvariant();
        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var timeframe = request.Timeframe.Trim().ToLowerInvariant();
        var ts = request.CandleTimestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        var baseKey = BuildBaseKey(symbol, timeframe, ts);

        if (request.Arguments.Count == 0)
        {
            return new IndicatorCacheKey($"{name}:{baseKey}", baseKey);
        }

        var builder = new StringBuilder();
        foreach (var entry in request.Arguments.OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(entry.Key.ToLowerInvariant());
            builder.Append('=');
            builder.Append(entry.Value.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
        }

        if (builder.Length > 0)
        {
            builder.Length--;
        }

        var args = builder.ToString();
        return new IndicatorCacheKey($"{name}:{baseKey}:{args}", baseKey);
    }

    public static string BuildBaseKey(string symbol, string timeframe, DateTimeOffset timestamp)
    {
        var symbolNorm = symbol.Trim().ToUpperInvariant();
        var timeframeNorm = timeframe.Trim().ToLowerInvariant();
        var ts = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return BuildBaseKey(symbolNorm, timeframeNorm, ts);
    }

    private static string BuildBaseKey(string normalizedSymbol, string normalizedTimeframe, string timestampSeconds)
        => $"{normalizedSymbol}:{normalizedTimeframe}:{timestampSeconds}";
}
