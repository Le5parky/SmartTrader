using Microsoft.EntityFrameworkCore;
using SmartTrader.Infrastructure.Persistence.Config;

namespace SmartTrader.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema.Core);

        modelBuilder.ApplyConfiguration(new UserConfig());
        modelBuilder.ApplyConfiguration(new SubscriptionConfig());
        modelBuilder.ApplyConfiguration(new SignalConfig());
        modelBuilder.ApplyConfiguration(new OutboxConfig());
        modelBuilder.ApplyConfiguration(new SymbolConfig());
        modelBuilder.ApplyConfiguration(new Candle1mConfig());
        modelBuilder.ApplyConfiguration(new IndicatorsCacheConfig());
    }
}


