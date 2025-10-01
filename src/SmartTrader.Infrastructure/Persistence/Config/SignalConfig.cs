using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class SignalConfig : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.ToTable("signals", Schema.Core);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Timeframe).IsRequired();
        builder.Property(e => e.Strategy).IsRequired();
        builder.Property(e => e.CandleTs).HasColumnName("candle_ts");
        builder.Property(e => e.Side).IsRequired();
        builder.Property(e => e.Price).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Confidence).HasColumnType("numeric(10, 4)");
        builder.Property(e => e.Reason);
        builder.Property(e => e.Snapshot).HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(e => new { e.SymbolId, e.Timeframe, e.Strategy, e.CandleTs })
            .IsUnique();
    }
}


