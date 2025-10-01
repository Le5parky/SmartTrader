using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription sub, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subscription sub, CancellationToken cancellationToken = default);
}


