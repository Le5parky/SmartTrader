using System.Collections.Generic;
using System.Text.Json;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;
using SmartTrader.Trading.Abstractions.Strategies;
using Trading.Strategies.BBands.Strategies;
using Trading.Strategies.RSI.Strategies;
using Xunit;

namespace SmartTrader.Trading.Tests.Strategies;

public sealed class StrategyTests
{
    [Fact]
    public async Task RsiBasicStrategy_GeneratesBuySignal()
    {
        var strategy = new RsiBasicStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 20m),
            ["EMA"] = IndicatorResult.FromSingleValue(CreateRequest("EMA", 50), 30m)
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 30m, 31m, 29m, 31m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 9, \"BuyLevel\": 25, \"SellLevel\": 75, \"TrendFilterEma\": 50, \"CooldownBars\": 3 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.Buy, result.Action);
        Assert.Equal(0.25m, result.Confidence);
        Assert.Equal(3, result.CooldownBars);
    }

    [Fact]
    public async Task RsiBasicStrategy_GeneratesSellSignal()
    {
        var strategy = new RsiBasicStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 80m),
            ["EMA"] = IndicatorResult.FromSingleValue(CreateRequest("EMA", 50), 30m)
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 30m, 31m, 29m, 29m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 9, \"BuyLevel\": 25, \"SellLevel\": 75, \"TrendFilterEma\": 50 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.Sell, result.Action);
        Assert.Equal(0.25m, result.Confidence);
    }

    [Fact]
    public async Task RsiBasicStrategy_RespectsTrendFilter()
    {
        var strategy = new RsiBasicStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 20m),
            ["EMA"] = IndicatorResult.FromSingleValue(CreateRequest("EMA", 50), 35m)
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 30m, 31m, 29m, 31m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 9, \"BuyLevel\": 25, \"SellLevel\": 75, \"TrendFilterEma\": 50 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.None, result.Action);
    }

    [Fact]
    public async Task BbReversalStrategy_GeneratesBuySignal()
    {
        var strategy = new BbReversalStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["BB"] = new IndicatorResult("BB", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["basis"] = 100m,
                ["lower"] = 95m,
                ["upper"] = 105m,
                ["value"] = 100m
            }),
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 20m),
            ["ADX"] = new IndicatorResult("ADX", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["value"] = 15m,
                ["plusDI"] = 25m,
                ["minusDI"] = 20m
            })
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 94m, 96m, 93m, 94m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 20, \"StdDev\": 2.0, \"RsiPeriod\": 9, \"RsiBuy\": 25, \"RsiSell\": 75, \"AdxPeriod\": 14, \"AdxThreshold\": 20 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.Buy, result.Action);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public async Task BbReversalStrategy_GeneratesSellSignal()
    {
        var strategy = new BbReversalStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["BB"] = new IndicatorResult("BB", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["basis"] = 100m,
                ["lower"] = 95m,
                ["upper"] = 105m,
                ["value"] = 100m
            }),
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 80m),
            ["ADX"] = new IndicatorResult("ADX", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["value"] = 15m,
                ["plusDI"] = 25m,
                ["minusDI"] = 20m
            })
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 106m, 107m, 105m, 106m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 20, \"StdDev\": 2.0, \"RsiPeriod\": 9, \"RsiBuy\": 25, \"RsiSell\": 75, \"AdxPeriod\": 14, \"AdxThreshold\": 20 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.Sell, result.Action);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public async Task BbReversalStrategy_RespectsAdxFilter()
    {
        var strategy = new BbReversalStrategy();
        var indicatorProvider = new FakeIndicatorProvider(new Dictionary<string, IndicatorResult>
        {
            ["BB"] = new IndicatorResult("BB", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["basis"] = 100m,
                ["lower"] = 95m,
                ["upper"] = 105m,
                ["value"] = 100m
            }),
            ["RSI"] = IndicatorResult.FromSingleValue(CreateRequest("RSI", 9), 20m),
            ["ADX"] = new IndicatorResult("ADX", "BTCUSDT", "1m", DateTimeOffset.UtcNow, new Dictionary<string, decimal>
            {
                ["value"] = 30m,
                ["plusDI"] = 30m,
                ["minusDI"] = 10m
            })
        });

        var context = new StrategyContext
        {
            Symbol = "BTCUSDT",
            Timeframe = "1m",
            CandleTimestamp = DateTimeOffset.UtcNow,
            History = new[] { new Candle(DateTimeOffset.UtcNow, 94m, 96m, 93m, 94m, 1m) },
            Indicators = indicatorProvider,
            Parameters = JsonDocument.Parse("{ \"Period\": 20, \"StdDev\": 2.0, \"RsiPeriod\": 9, \"RsiBuy\": 25, \"RsiSell\": 75, \"AdxPeriod\": 14, \"AdxThreshold\": 20 }")
        };

        var result = await strategy.EvaluateAsync(context, CancellationToken.None);

        Assert.Equal(TradeAction.None, result.Action);
    }

    private static IndicatorRequest CreateRequest(string name, int period)
    {
        return new IndicatorRequest(
            name,
            "BTCUSDT",
            "1m",
            DateTimeOffset.UtcNow,
            new Dictionary<string, decimal> { ["period"] = period });
    }

    private sealed class FakeIndicatorProvider : IIndicatorProvider
    {
        private readonly Dictionary<string, IndicatorResult> _results;

        public FakeIndicatorProvider(Dictionary<string, IndicatorResult> results)
        {
            _results = results;
        }

        public Task<IndicatorResult> GetAsync(IndicatorRequest request, CancellationToken cancellationToken)
        {
            if (_results.TryGetValue(request.Name, out var result))
            {
                return Task.FromResult(result);
            }

            throw new KeyNotFoundException($"No indicator configured for {request.Name}.");
        }
    }
}
