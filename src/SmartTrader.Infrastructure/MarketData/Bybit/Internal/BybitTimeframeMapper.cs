using SmartTrader.Domain.MarketData;

namespace SmartTrader.Infrastructure.MarketData.Bybit.Internal;

internal static class BybitTimeframeMapper
{
    private static readonly IReadOnlyDictionary<Timeframe, string> IntervalMap = new Dictionary<Timeframe, string>
    {
        [Timeframe.M1] = "1",
        [Timeframe.M3] = "3",
        [Timeframe.M5] = "5",
        [Timeframe.M15] = "15",
        [Timeframe.M30] = "30",
        [Timeframe.H1] = "60",
        [Timeframe.H4] = "240",
        [Timeframe.D1] = "D"
    };

    public static string ToInterval(Timeframe timeframe)
    {
        if (!IntervalMap.TryGetValue(timeframe, out var value))
        {
            throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe for Bybit integration.");
        }

        return value;
    }

    public static DateTime NormalizeWindowStart(Timeframe timeframe, DateTime timestampUtc)
    {
        return timeframe.AlignToFrame(timestampUtc.ToUniversalTime());
    }

    public static DateTime NormalizeWindowEnd(Timeframe timeframe, DateTime timestampUtc)
    {
        var aligned = NormalizeWindowStart(timeframe, timestampUtc);
        return aligned + timeframe.ToTimeSpan();
    }
}
