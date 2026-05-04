using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Assets;
using Shelter.Domain.Bookings;
using Shelter.Domain.Reviews;
using Shelter.Domain.Users;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Persistence;

public interface IShelterDbContext
{
    DbSet<ShelterEntity> Shelters { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Review> Reviews { get; }
    DbSet<Asset> Assets { get; }
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
