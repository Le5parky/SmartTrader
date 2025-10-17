using System.Collections.Generic;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class SmaCalculator : IIndicatorCalculator
{
    public string Name => "SMA";

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
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute SMA with period {period}.");
        }

        decimal sum = 0m;
        for (var i = candles.Count - period; i < candles.Count; i++)
        {
            sum += candles[i].Close;
        }

        var value = Math.Round(sum / period, 8, MidpointRounding.AwayFromZero);
        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = value
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, payload);
    }
}
