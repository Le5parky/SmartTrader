using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CryptoTrader.Domain.Entities;

namespace CryptoTrader.Infrastructure.Persistence.Config;

public class OpenPositionStateConfig : IEntityTypeConfiguration<OpenPositionState>
{
    public void Configure(EntityTypeBuilder<OpenPositionState> builder)
    {
        builder.ToTable("open_position_states", "public");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.MainOrderId).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.Side).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.EntryPrice).HasPrecision(18, 8);
        builder.Property(x => x.InitialStopLoss).HasPrecision(18, 8);
        builder.Property(x => x.CurrentStopLoss).HasPrecision(18, 8);
        builder.Property(x => x.TotalQuantity).HasPrecision(18, 8).HasDefaultValue(0);
        builder.Property(x => x.AccumulatedPnl).HasPrecision(18, 8).HasDefaultValue(0);
        builder.Property(x => x.IsClosed).HasDefaultValue(false);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamptz");

        builder.OwnsMany(x => x.TakeProfits, tp =>
        {
            tp.ToTable("position_take_profits", "public");
            tp.WithOwner().HasForeignKey("PositionStateId");
            tp.Property<int>("Id").ValueGeneratedOnAdd();
            tp.HasKey("Id");

            tp.Property(x => x.Sequence).IsRequired();
            tp.Property(x => x.TargetPrice).HasPrecision(18, 8);
            tp.Property(x => x.Quantity).HasPrecision(18, 8);
            tp.Property(x => x.IsTriggered).HasDefaultValue(false);
            tp.Property(x => x.TriggeredAtUtc).HasColumnType("timestamptz");
        });

        builder.HasIndex(x => x.Symbol)
               .HasDatabaseName("IX_open_position_states_Symbol")
               .IsUnique()
               .HasFilter("\"IsClosed\" = false");
    }
}
