namespace CryptoTrader.Application.Interfaces;

public interface IOrderService
{
    decimal GetTpRatio(int totalTps, int sequence);
}
