using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Strategies;

namespace Trading.Strategies.BBands.Strategies;

public sealed class BbReversalStrategy : IStrategy
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public string Name => "BB.Reversal";

    public Version Version => new(1, 0, 0);

    public async Task<StrategyResult> EvaluateAsync(StrategyContext ctx, CancellationToken ct)
    {
        if (ctx.History.Count == 0)
        {
            return StrategyResult.None(CreateSnapshot(new { Reason = "InsufficientHistory" }));
        }

        var parameters = Parameters.FromContext(ctx);

        var bbRequest = new IndicatorRequest(
            "BB",
            ctx.Symbol,
            ctx.Timeframe,
            ctx.CandleTimestamp,
            new Dictionary<string, decimal>
            {
                ["period"] = parameters.Period,
                ["stddev"] = parameters.StdDev
            });

        var bb = await ctx.Indicators.GetAsync(bbRequest, ct).ConfigureAwait(false);
        var basis = bb.TryGetValue("basis", out var basisValue) ? basisValue : bb.Value;
        var upper = bb.TryGetValue("upper", out var upperValue) ? upperValue : basis;
        var lower = bb.TryGetValue("lower", out var lowerValue) ? lowerValue : basis;

        var rsiRequest = new IndicatorRequest(
            "RSI",
            ctx.Symbol,
            ctx.Timeframe,
            ctx.CandleTimestamp,
            new Dictionary<string, decimal>
            {
                ["period"] = parameters.RsiPeriod
            });

        var rsi = await ctx.Indicators.GetAsync(rsiRequest, ct).ConfigureAwait(false);

        var adxRequest = new IndicatorRequest(
            "ADX",
            ctx.Symbol,
            ctx.Timeframe,
            ctx.CandleTimestamp,
            new Dictionary<string, decimal>
            {
                ["period"] = parameters.AdxPeriod
            });

        var adx = await ctx.Indicators.GetAsync(adxRequest, ct).ConfigureAwait(false);

        var lastCandle = ctx.History[^1];
        var close = lastCandle.Close;
        var cooldownBars = parameters.CooldownBars > 0 ? parameters.CooldownBars : (int?)null;

        var flatSnapshot = CreateSnapshot(new
        {
            BB = new { lower, basis, upper },
            RSI = rsi.Value,
            ADX = adx.Value,
            Close = close,
            Direction = "Flat"
        });

        if (adx.Value >= parameters.AdxThreshold)
        {
            return StrategyResult.None(flatSnapshot);
        }

        if (close <= lower && rsi.Value <= parameters.RsiBuy)
        {
            var confidence = ComputeLongConfidence(parameters, lower, basis, close, rsi.Value);
            var snapshot = CreateSnapshot(new
            {
                BB = new { lower, basis, upper },
                RSI = rsi.Value,
                ADX = adx.Value,
                Close = close,
                Direction = "Long"
            });

            return new StrategyResult(
                TradeAction.Buy,
                confidence,
                "Price<=LowerBand && RSI<Oversold && ADX<Threshold",
                snapshot,
                cooldownBars);
        }

        if (close >= upper && rsi.Value >= parameters.RsiSell)
        {
            var confidence = ComputeShortConfidence(parameters, upper, basis, close, rsi.Value);
            var snapshot = CreateSnapshot(new
            {
                BB = new { lower, basis, upper },
                RSI = rsi.Value,
                ADX = adx.Value,
                Close = close,
                Direction = "Short"
            });

            return new StrategyResult(
                TradeAction.Sell,
                confidence,
                "Price>=UpperBand && RSI>Overbought && ADX<Threshold",
                snapshot,
                cooldownBars);
        }

        return StrategyResult.None(flatSnapshot);
    }

    private static decimal ComputeLongConfidence(Parameters parameters, decimal lower, decimal basis, decimal close, decimal rsiValue)
    {
        var bandSpan = Math.Max(0.0000001m, basis - lower);
        var bandComponent = Math.Clamp((basis - close) / bandSpan, 0m, 1m);
        var rsiComponent = Math.Clamp((parameters.RsiBuy - rsiValue) / parameters.ConfidenceNormalization, 0m, 1m);
        return Math.Clamp((bandComponent + rsiComponent) / 2m, 0m, 1m);
    }

    private static decimal ComputeShortConfidence(Parameters parameters, decimal upper, decimal basis, decimal close, decimal rsiValue)
    {
        var bandSpan = Math.Max(0.0000001m, upper - basis);
        var bandComponent = Math.Clamp((close - basis) / bandSpan, 0m, 1m);
        var rsiComponent = Math.Clamp((rsiValue - parameters.RsiSell) / parameters.ConfidenceNormalization, 0m, 1m);
        return Math.Clamp((bandComponent + rsiComponent) / 2m, 0m, 1m);
    }

    private static JsonDocument CreateSnapshot<T>(T payload)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private sealed record Parameters
    {
        public int Period { get; init; } = 20;
        public decimal StdDev { get; init; } = 2m;
        public int RsiPeriod { get; init; } = 9;
        public decimal RsiBuy { get; init; } = 25m;
        public decimal RsiSell { get; init; } = 75m;
        public int AdxPeriod { get; init; } = 14;
        public decimal AdxThreshold { get; init; } = 20m;
        public decimal ConfidenceNormalization { get; init; } = 20m;
        public int CooldownBars { get; init; } = 0;

        public static Parameters FromContext(StrategyContext ctx)
        {
            try
            {
                if (ctx.Parameters.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var json = ctx.Parameters.RootElement.GetRawText();
                    return JsonSerializer.Deserialize<Parameters>(json, SerializerOptions) ?? new Parameters();
                }
            }
            catch (JsonException)
            {
                // ignore invalid payloads and fallback to defaults
            }

            return new Parameters();
        }
    }
}
