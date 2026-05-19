using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrader.Infrastructure.Persistence.Repositories;

public class PositionStateRepository : IPositionStateRepository
{
    private readonly IDbContextFactory<TradingDbContext> _contextFactory;

    public PositionStateRepository(IDbContextFactory<TradingDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(OpenPositionState state)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.OpenPositionStates.Add(state);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OpenPositionState state)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.OpenPositionStates.Update(state);
        await context.SaveChangesAsync();
    }

    public async Task<OpenPositionState?> GetOpenBySymbolAsync(string symbol)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OpenPositionStates
            .Include(x => x.TakeProfits)
            .FirstOrDefaultAsync(x => x.Symbol == symbol && !x.IsClosed);
    }

    public async Task<List<OpenPositionState>> GetOpenPositionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OpenPositionStates
            .Include(x => x.TakeProfits)
            .Where(x => !x.IsClosed)
            .ToListAsync();
    }
}
