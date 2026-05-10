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
        
        modelBuilder
            .HasDbFunction(typeof(TextFunctions)
                .GetMethod(nameof(TextFunctions.TrigramSimilarity))!)
            .HasName("similarity"); // Bind method TrigramSimilarity to (Postgres) SQL function 'similarity'

        // word_similarity is a different pg_trgm function (asymmetric, short-vs-long match);
        // see SearchShelterHandler for why one is used for names and the other for descriptions.
        modelBuilder
            .HasDbFunction(typeof(TextFunctions)
                .GetMethod(nameof(TextFunctions.TrigramWordSimilarity))!)
            .HasName("word_similarity"); // Bind method TrigramWordSimilarity to (Postgres) SQL function 'word_similarity'
    }
}
