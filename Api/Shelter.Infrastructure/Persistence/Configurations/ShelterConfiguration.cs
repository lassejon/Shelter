using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Spatial;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence.Configurations;

public sealed class ShelterConfiguration : IEntityTypeConfiguration<ShelterEntity>
{
    public void Configure(EntityTypeBuilder<ShelterEntity> builder)
    {
        builder.ToTable("Shelters");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(2000);

        builder.Property(s => s.Capacity)
            .IsRequired();

        builder.Property(s => s.Location)
            .HasColumnType($"geography (point, {SpatialReference.Wgs84})")
            .IsRequired();

        builder.Property(s => s.BookingPolicy)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.BookingApprovalMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.IsActive);
        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.UpdatedAt);
        builder.Property(s => s.OwnerId);

        builder.HasMany(s => s.Pictures)
            .WithOne()
            .HasForeignKey(p => p.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Pictures)
            .HasField("_pictures")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
