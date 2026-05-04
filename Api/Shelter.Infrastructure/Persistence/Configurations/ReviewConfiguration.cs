using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Reviews;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ShelterId);
        builder.Property(r => r.ReviewerId);

        builder.Property(r => r.Rating)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(4000);

        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.UpdatedAt);

        builder.HasOne<ShelterEntity>()
            .WithMany()
            .HasForeignKey(r => r.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Reviewer)
            .WithMany()
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Pictures)
            .WithOne()
            .HasForeignKey(p => p.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Pictures)
            .HasField("_pictures")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => new { r.ShelterId, r.ReviewerId }).IsUnique();
    }
}
