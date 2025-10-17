using System.Collections.Generic;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal sealed class RsiCalculator : IIndicatorCalculator
{
    public string Name => "RSI";

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
            throw new InvalidOperationException($"Insufficient candles ({candles.Count}) to compute RSI with period {period}.");
        }

        decimal gainSum = 0m;
        decimal lossSum = 0m;

        for (var i = 1; i <= period; i++)
        {
            var delta = candles[i].Close - candles[i - 1].Close;
            if (delta >= 0)
            {
                gainSum += delta;
            }
            else
            {
                lossSum += -delta;
            }
        }

        decimal avgGain = gainSum / period;
        decimal avgLoss = lossSum / period;

        for (var i = period + 1; i < candles.Count; i++)
        {
            var delta = candles[i].Close - candles[i - 1].Close;
            var gain = delta > 0 ? delta : 0m;
            var loss = delta < 0 ? -delta : 0m;

            avgGain = ((avgGain * (period - 1)) + gain) / period;
            avgLoss = ((avgLoss * (period - 1)) + loss) / period;
        }

        decimal rsi;
        if (avgLoss == 0m)
        {
            rsi = avgGain == 0m ? 50m : 100m;
        }
        else
        {
            var rs = avgGain / avgLoss;
            rsi = 100m - (100m / (1 + rs));
        }

        rsi = Math.Clamp(Math.Round(rsi, 2, MidpointRounding.AwayFromZero), 0m, 100m);

        var payload = new Dictionary<string, decimal>
        {
            [IndicatorResult.PrimaryValueKey] = rsi
        };

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, payload);
    }
}
