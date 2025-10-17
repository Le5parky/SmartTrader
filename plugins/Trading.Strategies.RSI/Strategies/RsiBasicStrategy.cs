using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Strategies;

namespace Trading.Strategies.RSI.Strategies;

public sealed class RsiBasicStrategy : IStrategy
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public string Name => "RSI.Basic";

    public Version Version => new(1, 0, 0);

    public async Task<StrategyResult> EvaluateAsync(StrategyContext ctx, CancellationToken ct)
    {
        if (ctx.History.Count == 0)
        {
            return StrategyResult.None(CreateSnapshot(new { Reason = "InsufficientHistory" }));
        }

        var parameters = Parameters.FromContext(ctx);

        var rsiRequest = new IndicatorRequest(
            "RSI",
            ctx.Symbol,
            ctx.Timeframe,
            ctx.CandleTimestamp,
            new Dictionary<string, decimal>
            {
                ["period"] = parameters.Period
            });

        var rsi = await ctx.Indicators.GetAsync(rsiRequest, ct).ConfigureAwait(false);

        decimal? emaValue = null;
        if (parameters.TrendFilterEma > 0)
        {
            var emaRequest = new IndicatorRequest(
                "EMA",
                ctx.Symbol,
                ctx.Timeframe,
                ctx.CandleTimestamp,
                new Dictionary<string, decimal>
                {
                    ["period"] = parameters.TrendFilterEma
                });

            emaValue = (await ctx.Indicators.GetAsync(emaRequest, ct).ConfigureAwait(false)).Value;
        }

        var lastCandle = ctx.History[^1];
        var close = lastCandle.Close;

        var cooldownBars = parameters.CooldownBars > 0 ? parameters.CooldownBars : (int?)null;

        bool TrendUp() => emaValue is null || close >= emaValue;
        bool TrendDown() => emaValue is null || close <= emaValue;

        if (rsi.Value <= parameters.BuyLevel && TrendUp())
        {
            var confidence = Math.Clamp((parameters.BuyLevel - rsi.Value) / parameters.ConfidenceNormalization, 0m, 1m);
            var snapshot = CreateSnapshot(new
            {
                RSI = rsi.Value,
                EMA = emaValue,
                Close = close,
                parameters.BuyLevel,
                parameters.SellLevel,
                parameters.TrendFilterEma,
                Direction = "Long"
            });

            return new StrategyResult(
                TradeAction.Buy,
                confidence,
                "RSI<Oversold && TrendUp",
                snapshot,
                cooldownBars);
        }

        if (rsi.Value >= parameters.SellLevel && TrendDown())
        {
            var confidence = Math.Clamp((rsi.Value - parameters.SellLevel) / parameters.ConfidenceNormalization, 0m, 1m);
            var snapshot = CreateSnapshot(new
            {
                RSI = rsi.Value,
                EMA = emaValue,
                Close = close,
                parameters.BuyLevel,
                parameters.SellLevel,
                parameters.TrendFilterEma,
                Direction = "Short"
            });

            return new StrategyResult(
                TradeAction.Sell,
                confidence,
                "RSI>Overbought && TrendDown",
                snapshot,
                cooldownBars);
        }

        var neutralSnapshot = CreateSnapshot(new
        {
            RSI = rsi.Value,
            EMA = emaValue,
            Close = close,
            parameters.BuyLevel,
            parameters.SellLevel,
            parameters.TrendFilterEma,
            Direction = "Flat"
        });

        return StrategyResult.None(neutralSnapshot);
    }

    private static JsonDocument CreateSnapshot<T>(T payload)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private sealed record Parameters
    {
        public int Period { get; init; } = 9;
        public decimal BuyLevel { get; init; } = 25m;
        public decimal SellLevel { get; init; } = 75m;
        public int TrendFilterEma { get; init; } = 50;
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
