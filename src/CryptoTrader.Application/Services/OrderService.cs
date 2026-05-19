using CryptoTrader.Application.Interfaces;

namespace CryptoTrader.Application.Services;

public class OrderService : IOrderService
{
    public decimal GetTpRatio(int totalTps, int sequence)
    {
        return totalTps switch
        {
            1 => 1.0m,
            2 => sequence == 1 ? 0.3m : 0.7m,
            3 => sequence == 1 ? 0.2m : sequence == 2 ? 0.3m : 0.5m,
            4 => sequence == 1 ? 0.1m : sequence == 2 ? 0.1m : sequence == 3 ? 0.3m : 0.5m,
            _ => 0
        };
    }
}
