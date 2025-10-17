using System.Collections.Generic;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class BollingerBandsCalculator : IIndicatorCalculator
{
    public string Name => "BB";

    public int GetWarmupCandleCount(IndicatorRequest request)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        return period;
    }

    public IndicatorResult Calculate(IndicatorRequest request, IReadOnlyList<Candle> candles)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        if (candles.Count < period)
        {
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute Bollinger Bands with period {period}.");
        }

        var multiplier = request.GetOptionalDecimal("stddev", 2m);

        decimal sum = 0m;
        for (var i = candles.Count - period; i < candles.Count; i++)
        {
            sum += candles[i].Close;
        }

        var mean = sum / period;

        decimal varianceSum = 0m;
        for (var i = candles.Count - period; i < candles.Count; i++)
        {
            var diff = candles[i].Close - mean;
            varianceSum += diff * diff;
        }

        var variance = varianceSum / period;
        var stdDev = (decimal)Math.Sqrt((double)variance);

        var upper = mean + multiplier * stdDev;
        var lower = mean - multiplier * stdDev;

        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = Math.Round(mean, 8, MidpointRounding.AwayFromZero),
            ["basis"] = Math.Round(mean, 8, MidpointRounding.AwayFromZero),
            ["upper"] = Math.Round(upper, 8, MidpointRounding.AwayFromZero),
            ["lower"] = Math.Round(lower, 8, MidpointRounding.AwayFromZero),
            ["stdDev"] = Math.Round(stdDev, 8, MidpointRounding.AwayFromZero)
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, payload);
    }
}
