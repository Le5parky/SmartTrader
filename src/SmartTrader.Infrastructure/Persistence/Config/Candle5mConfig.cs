using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class Candle5mConfig : IEntityTypeConfiguration<Candle5m>
{
    public void Configure(EntityTypeBuilder<Candle5m> builder)
    {
        builder.ToTable("candles_5m", Schema.Market);

        builder.HasKey(e => new { e.SymbolId, e.TsOpen });
        builder.Property(e => e.TsOpen).HasColumnName("ts_open");

        builder.Property(e => e.Open).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.High).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Low).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Close).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Volume).HasColumnType("numeric(38, 10)");

        builder.HasIndex(e => new { e.SymbolId, e.TsOpen }).HasDatabaseName("ix_candles_5m_symbol_tsopen_desc");
    }
}
