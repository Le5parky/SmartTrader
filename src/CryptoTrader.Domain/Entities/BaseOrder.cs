using CryptoTrader.Domain.Enums;

namespace CryptoTrader.Domain.Entities;

public class BaseOrder
{
    public OrderSide Side { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal InputPrice { get; set; }
    public decimal StopLoss { get; set; }
    public List<TakeProfitSignal> TakeProfits { get; set; } = new();
}

public class TakeProfitSignal
{
    public decimal Price { get; set; }
    public decimal Amount { get; set; } // Percentage share
}
