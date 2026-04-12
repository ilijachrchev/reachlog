using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReachLog.Domain.Entities;

namespace ReachLog.Infrastructure.Persistence.Configurations;

public class OutreachConfiguration : IEntityTypeConfiguration<Outreach>
{
    public void Configure(EntityTypeBuilder<Outreach> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.ContactEmail)
            .HasMaxLength(256);

        builder.Property(o => o.Role)
            .HasMaxLength(200);

        builder.Property(o => o.Channel)
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
    }
}