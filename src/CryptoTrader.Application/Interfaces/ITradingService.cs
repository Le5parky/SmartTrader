using CryptoTrader.Domain.Entities;

namespace CryptoTrader.Application.Interfaces;

public interface ITradingService
{
    Task<string> CreateOrderAsync(BaseOrder baseOrder);
}
