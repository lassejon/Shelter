using App.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Bookings;
using Shelter.Domain.Reviews;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Persistence;

public sealed class ShelterDbContext(DbContextOptions<ShelterDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IShelterDbContext
{
    public DbSet<ShelterEntity> Shelters => Set<ShelterEntity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Asset> Assets => Set<Asset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelterDbContext).Assembly);

        // Map App.Persistence.TextFunctions.TrigramSimilarity → Postgres pg_trgm similarity(a, b).
        // App stays provider-agnostic; this binding is the only Postgres-specific bit.
        modelBuilder
            .HasDbFunction(typeof(TextFunctions)
                .GetMethod(nameof(TextFunctions.TrigramSimilarity))!)
            .HasName("similarity");
    }
}
