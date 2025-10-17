using System.Collections.Generic;
using System.Linq;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class AdxCalculator : IIndicatorCalculator
{
    public string Name => "ADX";

    public int GetWarmupCandleCount(IndicatorRequest request)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        return period * 2;
    }

    public IndicatorResult Calculate(IndicatorRequest request, IReadOnlyList<Candle> candles)
    {
        var period = Math.Max(1, request.GetRequiredInt("period"));
        if (candles.Count < period * 2)
        {
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute ADX with period {period}. Require at least {period * 2}.");
        }

        decimal smoothedTr = 0m;
        decimal smoothedPlusDm = 0m;
        decimal smoothedMinusDm = 0m;

        decimal? latestPlusDi = null;
        decimal? latestMinusDi = null;
        var dxSeries = new List<decimal>();

        for (var i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];

            var upMove = current.High - previous.High;
            var downMove = previous.Low - current.Low;

            var plusDm = upMove > downMove && upMove > 0m ? upMove : 0m;
            var minusDm = downMove > upMove && downMove > 0m ? downMove : 0m;

            var tr = TrueRange(current, previous.Close);

            if (i <= period)
            {
                smoothedTr += tr;
                smoothedPlusDm += plusDm;
                smoothedMinusDm += minusDm;

                if (i == period)
                {
                    smoothedTr /= period;
                    smoothedPlusDm /= period;
                    smoothedMinusDm /= period;
                }
            }
            else
            {
                smoothedTr = ((smoothedTr * (period - 1)) + tr) / period;
                smoothedPlusDm = ((smoothedPlusDm * (period - 1)) + plusDm) / period;
                smoothedMinusDm = ((smoothedMinusDm * (period - 1)) + minusDm) / period;
            }

            if (i >= period)
            {
                decimal plusDi = 0m;
                decimal minusDi = 0m;
                if (smoothedTr > 0)
                {
                    plusDi = 100m * (smoothedPlusDm / smoothedTr);
                    minusDi = 100m * (smoothedMinusDm / smoothedTr);
                }

                latestPlusDi = plusDi;
                latestMinusDi = minusDi;

                var dx = CalculateDx(plusDi, minusDi);
                dxSeries.Add(dx);
            }
        }

        if (dxSeries.Count < period)
        {
            throw new InvalidOperationException($"Unable to compute ADX: collected only {dxSeries.Count} DX values for period {period}.");
        }

        decimal adx = dxSeries.Take(period).Sum() / period;
        for (var i = period; i < dxSeries.Count; i++)
        {
            adx = ((adx * (period - 1)) + dxSeries[i]) / period;
        }

        adx = Math.Round(adx, 2, MidpointRounding.AwayFromZero);
        var plus = Math.Round(latestPlusDi ?? 0m, 2, MidpointRounding.AwayFromZero);
        var minus = Math.Round(latestMinusDi ?? 0m, 2, MidpointRounding.AwayFromZero);

        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = adx,
            ["plusDI"] = plus,
            ["minusDI"] = minus
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

    private static decimal CalculateDx(decimal plusDi, decimal minusDi)
    {
        var denominator = plusDi + minusDi;
        if (denominator == 0m)
        {
            return 0m;
        }

        return 100m * Math.Abs(plusDi - minusDi) / denominator;
    }
}
