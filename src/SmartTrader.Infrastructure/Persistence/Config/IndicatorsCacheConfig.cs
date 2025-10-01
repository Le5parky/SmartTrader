using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class IndicatorsCacheConfig : IEntityTypeConfiguration<IndicatorsCache>
{
    public void Configure(EntityTypeBuilder<IndicatorsCache> builder)
    {
        builder.ToTable("indicators_cache", Schema.Market);

        builder.HasKey(e => new { e.SymbolId, e.Timeframe, e.Name, e.CandleTs });

        builder.Property(e => e.CandleTs).HasColumnName("candle_ts");
        builder.Property(e => e.Values).HasColumnType("jsonb");
    }
}


