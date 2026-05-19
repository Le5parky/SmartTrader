using Microsoft.EntityFrameworkCore;
using CryptoTrader.Domain.Entities;
using CryptoTrader.Infrastructure.Persistence.Config;

namespace CryptoTrader.Infrastructure.Persistence;

public class TradingDbContext : DbContext
{
    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options)
    {
    }

    public DbSet<OpenPositionState> OpenPositionStates => Set<OpenPositionState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfiguration(new OpenPositionStateConfig());
    }
}
