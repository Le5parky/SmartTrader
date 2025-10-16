using System.Diagnostics.CodeAnalysis;

namespace SmartTrader.Domain.MarketData;

public static class TimeframeExtensions
{
    public static TimeSpan ToTimeSpan(this Timeframe timeframe) => timeframe switch
    {
        Timeframe.M1 => TimeSpan.FromMinutes(1),
        Timeframe.M3 => TimeSpan.FromMinutes(3),
        Timeframe.M5 => TimeSpan.FromMinutes(5),
        Timeframe.M15 => TimeSpan.FromMinutes(15),
        Timeframe.M30 => TimeSpan.FromMinutes(30),
        Timeframe.H1 => TimeSpan.FromHours(1),
        Timeframe.H4 => TimeSpan.FromHours(4),
        Timeframe.D1 => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe")
    };

    public static string ToLabel(this Timeframe timeframe) => timeframe switch
    {
        Timeframe.M1 => "1m",
        Timeframe.M3 => "3m",
        Timeframe.M5 => "5m",
        Timeframe.M15 => "15m",
        Timeframe.M30 => "30m",
        Timeframe.H1 => "1h",
        Timeframe.H4 => "4h",
        Timeframe.D1 => "1d",
        _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe")
    };

    public static bool TryParse(string label, [NotNullWhen(true)] out Timeframe? timeframe)
    {
        timeframe = label?.ToLowerInvariant() switch
        {
            "1m" => Timeframe.M1,
            "3m" => Timeframe.M3,
            "5m" => Timeframe.M5,
            "15m" => Timeframe.M15,
            "30m" => Timeframe.M30,
            "1h" or "60m" => Timeframe.H1,
            "4h" => Timeframe.H4,
            "1d" => Timeframe.D1,
            _ => null
        };

        return timeframe.HasValue;
    }

    public static DateTime AlignToFrame(this Timeframe timeframe, DateTime timestampUtc)
    {
        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            timestampUtc = DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc);
        }

        var span = timeframe.ToTimeSpan();
        var ticks = (timestampUtc.Ticks / span.Ticks) * span.Ticks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
