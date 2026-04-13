using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReachLog.Domain.Entities;

namespace ReachLog.Infrastructure.Persistence.Configurations;

public class UserCvConfiguration : IEntityTypeConfiguration<UserCv>
{
    public void Configure(EntityTypeBuilder<UserCv> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ExtractedText)
            .IsRequired();

        builder.HasIndex(c => c.UserId)
            .IsUnique();

        builder.HasOne(c => c.User)
            .WithOne()
            .HasForeignKey<UserCv>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}