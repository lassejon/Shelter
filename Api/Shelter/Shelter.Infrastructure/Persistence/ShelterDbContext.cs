using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shelter.Application.Interfaces;
using Shelter.Domain.Bookings;
using Shelter.Domain.Pictures;
using ShelterModel = Shelter.Domain.Shelters.Shelter;
using Shelter.Domain.Shelters;
using Shelter.Domain.Users;

namespace Shelter.Infrastructure.Persistence;

public class ShelterDbContext(DbContextOptions<ShelterDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options), IUnitOfWork, IRepository
{
    public override DbSet<User> Users { get; set; } = null!;
    public DbSet<ShelterModel> Shelters { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<Picture> Pictures { get; set; } = null!;

    public async Task CommitChangesAsync()
    {
        await base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShelterDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}
