using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartTrader.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // For migrations, prefer env var; fallback to local dev default
        var conn = Environment.GetEnvironmentVariable("MIGRATIONS_CONNECTION")
                   ?? "Host=localhost;Port=5432;Database=smarttrader_core;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(conn);
        return new AppDbContext(optionsBuilder.Options);
    }
}


