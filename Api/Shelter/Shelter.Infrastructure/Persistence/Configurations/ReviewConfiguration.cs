using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Shelters;

namespace Shelter.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Rating)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(4000);

        builder.HasOne(x => x.Shelter)
            .WithMany(s => s.Reviews)
            .HasForeignKey(x => x.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Pictures)
            .WithOne(p => p.Review)
            .HasForeignKey(p => p.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ShelterId, x.ReviewerId });
    }
}
