using System.Collections.Generic;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class AtrCalculator : IIndicatorCalculator
{
    public string Name => "ATR";

    public int GetWarmupCandleCount(IndicatorRequest request)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        return period + 1;
    }

    public IndicatorResult Calculate(IndicatorRequest request, IReadOnlyList<Candle> candles)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        if (candles.Count < period + 1)
        {
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute ATR with period {period}.");
        }

        decimal atr = 0m;
        var prevClose = candles[0].Close;

        for (var i = 1; i < candles.Count; i++)
        {
            var candle = candles[i];
            var tr = TrueRange(candle, prevClose);

            if (i <= period)
            {
                atr += tr;
                if (i == period)
                {
                    atr /= period;
                }
            }
            else
            {
                atr = ((atr * (period - 1)) + tr) / period;
            }

            prevClose = candle.Close;
        }

        atr = Math.Round(atr, 8, MidpointRounding.AwayFromZero);

        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = atr
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, payload);
    }

    private static decimal TrueRange(Candle candle, decimal previousClose)
    {
        var rangeHighLow = candle.High - candle.Low;
        var rangeHighClose = Math.Abs(candle.High - previousClose);
        var rangeLowClose = Math.Abs(candle.Low - previousClose);
        return Math.Max(rangeHighLow, Math.Max(rangeHighClose, rangeLowClose));
    }
}
