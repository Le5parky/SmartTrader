using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Config;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", Schema.Core);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ChatId)
            .IsRequired();

        builder.HasIndex(e => e.ChatId)
            .IsUnique();

        builder.Property(e => e.Role)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}


