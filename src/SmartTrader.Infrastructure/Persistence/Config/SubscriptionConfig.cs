using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class SubscriptionConfig : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions", Schema.Core);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Timeframe).IsRequired();
        builder.Property(e => e.Strategy).IsRequired();

        builder.Property(e => e.Params)
            .HasColumnType("jsonb")
            .HasColumnName("params");

        builder.Property(e => e.Active)
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(e => new { e.UserId, e.SymbolId, e.Timeframe, e.Strategy })
            .IsUnique();

        builder.HasOne(e => e.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


