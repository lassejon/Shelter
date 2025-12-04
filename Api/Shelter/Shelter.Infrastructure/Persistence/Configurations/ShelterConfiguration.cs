using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShelterModel = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence.Configurations;

public class ShelterConfiguration : IEntityTypeConfiguration<ShelterModel>
{
    public void Configure(EntityTypeBuilder<ShelterModel> builder)
    {
        builder.ToTable("Shelters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.BookingPolicy)
            .HasConversion<int>()  // store as smallint/int
            .IsRequired();

        builder.Property(x => x.Location)
            .HasColumnType("geometry (point)")
            .IsRequired();

        builder.HasOne(x => x.Owner)
            .WithMany(u => u.OwnedShelters)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Bookings)
            .WithOne(b => b.Shelter)
            .HasForeignKey(b => b.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(r => r.Shelter)
            .HasForeignKey(r => r.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Pictures)
            .WithOne(p => p.Shelter)
            .HasForeignKey(p => p.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
