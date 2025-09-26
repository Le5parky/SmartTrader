using Microsoft.EntityFrameworkCore;

namespace SmartTrader.Infrastructure.Persistence;

public class CoreDbContext : DbContext
{
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure core schema entities here (Users, Subscriptions, Signals, Outbox)
        // Keep empty for initial scaffold
    }
}


