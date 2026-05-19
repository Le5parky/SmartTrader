using CryptoTrader.Domain.Entities;

namespace CryptoTrader.Domain.Interfaces;

public interface IPositionStateRepository
{
    Task AddAsync(OpenPositionState state);
    Task UpdateAsync(OpenPositionState state);
    Task<OpenPositionState?> GetOpenBySymbolAsync(string symbol);
    Task<List<OpenPositionState>> GetOpenPositionsAsync();
}
