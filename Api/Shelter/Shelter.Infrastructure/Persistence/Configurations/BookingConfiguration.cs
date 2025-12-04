using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shelter.Domain.Bookings;

namespace Shelter.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Guests)
            .IsRequired();

        builder.Property(x => x.StartUtc)
            .IsRequired();

        builder.Property(x => x.EndUtc)
            .IsRequired();

        builder.HasOne(x => x.Shelter)
            .WithMany(s => s.Bookings)
            .HasForeignKey(x => x.ShelterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Booker)
            .WithMany(u => u.Bookings)
            .HasForeignKey(x => x.BookerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new { x.ShelterId, x.StartUtc, x.EndUtc });
    }
}
