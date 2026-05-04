using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Bookings;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ShelterId);
        builder.Property(b => b.BookerId);
        builder.Property(b => b.StartUtc).IsRequired();
        builder.Property(b => b.EndUtc).IsRequired();
        builder.Property(b => b.Guests).IsRequired();

        builder.Property(b => b.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.CreatedAt);
        builder.Property(b => b.UpdatedAt);

        builder.HasOne<ShelterEntity>()
            .WithMany()
            .HasForeignKey(b => b.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Booker)
            .WithMany()
            .HasForeignKey(b => b.BookerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.ShelterId, b.StartUtc, b.EndUtc });
    }
}
