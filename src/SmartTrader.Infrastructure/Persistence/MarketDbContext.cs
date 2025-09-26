using Microsoft.EntityFrameworkCore;

namespace SmartTrader.Infrastructure.Persistence;

public class MarketDbContext : DbContext
{
    public MarketDbContext(DbContextOptions<MarketDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure market schema entities here (Candles, Symbols)
        // Keep empty for initial scaffold
    }
}


