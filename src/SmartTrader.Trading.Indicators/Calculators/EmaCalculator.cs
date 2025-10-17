using System.Collections.Generic;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class EmaCalculator : IIndicatorCalculator
{
    public string Name => "EMA";

    public int GetWarmupCandleCount(IndicatorRequest request)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        return Math.Max(period * 5, period + 1);
    }

    public IndicatorResult Calculate(IndicatorRequest request, IReadOnlyList<Candle> candles)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        if (candles.Count < period)
        {
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute EMA with period {period}.");
        }

        var smoothing = 2m / (period + 1);

        decimal sum = 0m;
        for (var i = 0; i < period; i++)
        {
            sum += candles[i].Close;
        }

        decimal ema = sum / period;
        for (var i = period; i < candles.Count; i++)
        {
            var close = candles[i].Close;
            ema = ((close - ema) * smoothing) + ema;
        }

        ema = Math.Round(ema, 8, MidpointRounding.AwayFromZero);
        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = ema
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, payload);
    }
}
