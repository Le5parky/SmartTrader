using System;
using System.Collections.Generic;
using System.Linq;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;
using SmartTrader.Trading.Indicators.Calculators;
using Xunit;

namespace SmartTrader.Trading.Tests.Indicators;

public sealed class IndicatorCalculatorTests
{
    [Fact]
    public void SmaCalculator_ComputesExpectedValue()
    {
        var candles = CreateCandles(new[] { 1m, 2m, 3m, 4m, 5m });
        var request = CreateRequest("SMA", 3);

        var calculator = new SmaCalculator();
        var result = calculator.Calculate(request, candles);

        Assert.Equal(4m, result.Value);
    }

    [Fact]
    public void EmaCalculator_ComputesExpectedValue()
    {
        var candles = CreateCandles(new[] { 1m, 2m, 3m, 4m, 5m });
        var request = CreateRequest("EMA", 3);

        var calculator = new EmaCalculator();
        var result = calculator.Calculate(request, candles);

        Assert.Equal(4m, Math.Round(result.Value, 2));
    }

    [Fact]
    public void RsiCalculator_MatchesReferenceSample()
    {
        var closes = new[]
        {
            44.34m, 44.09m, 44.15m, 43.61m, 44.33m, 44.83m, 45.10m,
            45.42m, 45.84m, 46.08m, 45.89m, 46.03m, 45.61m, 46.28m, 46.28m
        };

        var candles = CreateCandles(closes);
        var request = CreateRequest("RSI", 14);

        var calculator = new RsiCalculator();
        var result = calculator.Calculate(request, candles);

        Assert.Equal(70.46m, Math.Round(result.Value, 2));
    }

    [Fact]
    public void BollingerBandsCalculator_ComputesExpectedBands()
    {
        var closes = new[] { 20m, 21m, 22m, 23m, 24m, 25m, 26m, 27m, 28m, 29m };
        var candles = CreateCandles(closes);
        var request = new IndicatorRequest(
            "BB",
            "BTCUSDT",
            "1m",
            candles.Last().TsOpenUtc,
            new Dictionary<string, decimal>
            {
                ["period"] = 5,
                ["stddev"] = 2m
            });

        var calculator = new BollingerBandsCalculator();
        var result = calculator.Calculate(request, candles);

        var basis = closes[^5..].Average();
        var std = (decimal)Math.Sqrt((double)closes[^5..]
            .Select(v => (v - basis) * (v - basis))
            .Average());

        Assert.Equal(Math.Round(basis, 8), Math.Round(result.Value, 8));
        Assert.Equal(Math.Round(basis + 2m * std, 8), Math.Round(result.Values["upper"], 8));
        Assert.Equal(Math.Round(basis - 2m * std, 8), Math.Round(result.Values["lower"], 8));
    }

    [Fact]
    public void AtrCalculator_ComputesExpectedValue()
    {
        var candles = CreateCandles(
            closes: new[] { 9m, 10m, 11m, 13m },
            highs: new[] { 10m, 11m, 12m, 14m },
            lows: new[] { 8m, 9m, 9m, 10m });

        var request = CreateRequest("ATR", 3);
        var calculator = new AtrCalculator();
        var result = calculator.Calculate(request, candles);

        Assert.Equal(3m, Math.Round(result.Value, 4));
    }

    [Fact]
    public void AdxCalculator_ComputesExpectedValue()
    {
        var candles = CreateCandles(
            closes: new[] { 29m, 31m, 30.5m, 32m, 34m, 35m, 33.5m },
            highs: new[] { 30m, 32m, 31m, 33m, 35m, 36m, 34m },
            lows: new[] { 28m, 29m, 30m, 31m, 33m, 34m, 33m });

        var request = CreateRequest("ADX", 3);
        var calculator = new AdxCalculator();
        var result = calculator.Calculate(request, candles);

        var expected = ComputeAdxExpected(candles, 3);
        Assert.Equal(Math.Round(expected, 2), Math.Round(result.Value, 2));
    }

    private static IndicatorRequest CreateRequest(string name, int period)
    {
        return new IndicatorRequest(
            name,
            "BTCUSDT",
            "1m",
            DateTimeOffset.UtcNow,
            new Dictionary<string, decimal>
            {
                ["period"] = period
            });
    }

    private static IReadOnlyList<Candle> CreateCandles(
        decimal[] closes,
        decimal[]? highs = null,
        decimal[]? lows = null)
    {
        var candles = new List<Candle>(closes.Length);
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < closes.Length; i++)
        {
            var close = closes[i];
            var high = highs is not null ? highs[i] : close;
            var low = lows is not null ? lows[i] : close;
            var open = low;

            candles.Add(new Candle(
                baseTime.AddMinutes(i),
                open,
                high,
                low,
                close,
                1m));
        }

        return candles;
    }

    private static decimal ComputeAdxExpected(IReadOnlyList<Candle> candles, int period)
    {
        var dmPlus = new List<decimal>();
        var dmMinus = new List<decimal>();
        var trueRanges = new List<decimal>();

        for (var i = 1; i < candles.Count; i++)
        {
            var current = candles[i];
            var previous = candles[i - 1];

            var upMove = current.High - previous.High;
            var downMove = previous.Low - current.Low;

            dmPlus.Add(upMove > downMove && upMove > 0 ? upMove : 0m);
            dmMinus.Add(downMove > upMove && downMove > 0 ? downMove : 0m);

            var tr = Math.Max(
                current.High - current.Low,
                Math.Max(Math.Abs(current.High - previous.Close), Math.Abs(current.Low - previous.Close)));
            trueRanges.Add(tr);
        }

        var smoothedTr = trueRanges.Take(period).Average();
        var smoothedPlus = dmPlus.Take(period).Average();
        var smoothedMinus = dmMinus.Take(period).Average();

        var dxValues = new List<decimal>();
        for (var i = 0; i < trueRanges.Count; i++)
        {
            if (i >= period)
            {
                smoothedTr = ((smoothedTr * (period - 1)) + trueRanges[i]) / period;
                smoothedPlus = ((smoothedPlus * (period - 1)) + dmPlus[i]) / period;
                smoothedMinus = ((smoothedMinus * (period - 1)) + dmMinus[i]) / period;
            }

            if (i >= period - 1)
            {
                var plusDi = smoothedTr == 0 ? 0m : 100m * (smoothedPlus / smoothedTr);
                var minusDi = smoothedTr == 0 ? 0m : 100m * (smoothedMinus / smoothedTr);
                var denominator = plusDi + minusDi;
                var dx = denominator == 0 ? 0m : 100m * Math.Abs(plusDi - minusDi) / denominator;
                dxValues.Add(dx);
            }
        }

        var adx = dxValues.Take(period).Average();
        for (var i = period; i < dxValues.Count; i++)
        {
            adx = ((adx * (period - 1)) + dxValues[i]) / period;
        }

        return adx;
    }
}
