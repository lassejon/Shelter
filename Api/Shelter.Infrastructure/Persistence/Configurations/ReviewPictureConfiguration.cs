using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Reviews;

namespace Shelter.Infrastructure.Persistence.Configurations;

public sealed class ReviewPictureConfiguration : IEntityTypeConfiguration<ReviewPicture>
{
    public void Configure(EntityTypeBuilder<ReviewPicture> builder)
    {
        builder.ToTable("ReviewPictures");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Caption)
            .HasMaxLength(400);

        builder.Property(p => p.SortOrder);
        builder.Property(p => p.ReviewId);
        builder.Property(p => p.AssetId);

        builder.HasOne(p => p.Asset)
            .WithMany()
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.AssetId);
    }
}
