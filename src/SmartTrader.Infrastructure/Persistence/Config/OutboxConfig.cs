using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class OutboxConfig : IEntityTypeConfiguration<Outbox>
{
    public void Configure(EntityTypeBuilder<Outbox> builder)
    {
        builder.ToTable("outbox", Schema.Core);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.SentAt).HasColumnName("sent_at");
        builder.Property(e => e.Status).HasMaxLength(16).HasColumnName("status");

        builder.HasIndex(e => new { e.Status, e.CreatedAt });
    }
}


