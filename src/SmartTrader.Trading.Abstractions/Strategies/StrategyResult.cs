using System.Text.Json;

namespace SmartTrader.Trading.Abstractions.Strategies;

public sealed class StrategyResult
{
    public StrategyResult(
        TradeAction action,
        decimal confidence,
        string? reason,
        JsonDocument snapshot,
        int? cooldownBars = null)
    {
        if (confidence is < 0m or > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be within [0,1].");
        }

        Action = action;
        Confidence = confidence;
        Reason = reason;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        CooldownBars = cooldownBars;
    }

    public TradeAction Action { get; }
    public decimal Confidence { get; }
    public string? Reason { get; }
    public JsonDocument Snapshot { get; }
    public int? CooldownBars { get; }

    public static StrategyResult None(JsonDocument? snapshot = null) =>
        new(TradeAction.None, 0m, null, snapshot ?? JsonDocument.Parse("{}"));
}
