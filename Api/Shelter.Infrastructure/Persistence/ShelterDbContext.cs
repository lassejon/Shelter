using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence;

public sealed class ShelterDbContext(DbContextOptions<ShelterDbContext> options)
    : DbContext(options), IShelterDbContext
{
    public DbSet<ShelterEntity> Shelters => Set<ShelterEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShelterEntity>(shelter =>
        {
            shelter.HasKey(s => s.Id);

            shelter.Property(s => s.Name).IsRequired();
            shelter.Property(s => s.Description);
            shelter.Property(s => s.Capacity);
            shelter.Property(s => s.Latitude);
            shelter.Property(s => s.Longitude);
            shelter.Property(s => s.BookingPolicy);
            shelter.Property(s => s.IsActive);
            shelter.Property(s => s.CreatedAt);
            shelter.Property(s => s.UpdatedAt);
            shelter.Property(s => s.OwnerId);

            shelter.HasMany(s => s.Pictures)
                .WithOne()
                .HasForeignKey(p => p.ShelterId)
                .OnDelete(DeleteBehavior.Cascade);

            shelter.Navigation(s => s.Pictures)
                .HasField("_pictures")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ShelterPicture>(picture =>
        {
            picture.HasKey(p => p.Id);

            picture.Property(p => p.Url).IsRequired();
            picture.Property(p => p.Caption);
            picture.Property(p => p.SortOrder);
            picture.Property(p => p.ShelterId);
        });
    }
}
