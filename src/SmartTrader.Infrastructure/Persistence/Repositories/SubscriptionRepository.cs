using Microsoft.EntityFrameworkCore;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _dbContext;

    public SubscriptionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Subscription>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Subscription>()
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Subscription sub, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Subscription>().AddAsync(sub, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Subscription sub, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Subscription>().Update(sub);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


