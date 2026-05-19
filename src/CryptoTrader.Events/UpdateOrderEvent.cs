using CryptoTrader.Domain.Entities;
using Bybit.Net.Enums;

namespace CryptoTrader.Events;

public class UpdateOrderEvent : IEvent
{
    public string Symbol { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
}
