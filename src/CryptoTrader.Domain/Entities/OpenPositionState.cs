using CryptoTrader.Domain.Enums;

namespace CryptoTrader.Domain.Entities;

public class OpenPositionState
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string MainOrderId { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal InitialStopLoss { get; set; }
    public decimal CurrentStopLoss { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AccumulatedPnl { get; set; }
    public bool IsClosed { get; set; }
    public int PositionIdx { get; set; } // 0 for one-way
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<TakeProfitState> TakeProfits { get; set; } = new();

    public void Close(DateTime utcNow)
    {
        IsClosed = true;
        UpdatedAtUtc = utcNow;
    }

    public bool TryAdvanceStopLoss(decimal triggerPrice, DateTime utcNow, out int sequence, out decimal? newStopLoss)
    {
        sequence = 0;
        newStopLoss = null;

        TakeProfitState? tp = null;
        foreach (var t in TakeProfits)
        {
            if (!t.IsTriggered && t.TargetPrice == triggerPrice)
            {
                if (tp == null || t.Sequence < tp.Sequence)
                {
                    tp = t;
                }
            }
        }

        if (tp == null) return false;

        tp.IsTriggered = true;
        tp.TriggeredAtUtc = utcNow;
        sequence = tp.Sequence;

        int currentSequence = sequence;

        if (currentSequence == 2)
        {
            newStopLoss = EntryPrice;
        }
        else if (currentSequence >= 3)
        {
            foreach (var t in TakeProfits)
            {
                if (t.Sequence == currentSequence - 1)
                {
                    newStopLoss = t.TargetPrice;
                    break;
                }
            }
        }

        if (newStopLoss.HasValue)
        {
            CurrentStopLoss = newStopLoss.Value;
        }

        UpdatedAtUtc = utcNow;

        bool allTriggered = true;
        foreach (var t in TakeProfits)
        {
            if (!t.IsTriggered)
            {
                allTriggered = false;
                break;
            }
        }

        if (allTriggered)
        {
            IsClosed = true;
        }

        return true;
    }
}

public class TakeProfitState
{
    public int Sequence { get; set; }
    public decimal TargetPrice { get; set; }
    public decimal Quantity { get; set; }
    public bool IsTriggered { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
}
