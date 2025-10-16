using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class Candle15mConfig : IEntityTypeConfiguration<Candle15m>
{
    public void Configure(EntityTypeBuilder<Candle15m> builder)
    {
        builder.ToTable("candles_15m", Schema.Market);

        builder.HasKey(e => new { e.SymbolId, e.TsOpen });
        builder.Property(e => e.TsOpen).HasColumnName("ts_open");

        builder.Property(e => e.Open).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.High).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Low).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Close).HasColumnType("numeric(38, 10)");
        builder.Property(e => e.Volume).HasColumnType("numeric(38, 10)");

        builder.HasIndex(e => new { e.SymbolId, e.TsOpen }).HasDatabaseName("ix_candles_15m_symbol_tsopen_desc");
    }
}
