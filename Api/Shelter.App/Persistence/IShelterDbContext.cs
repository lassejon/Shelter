using Microsoft.EntityFrameworkCore;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Persistence;

public interface IShelterDbContext
{
    DbSet<ShelterEntity> Shelters { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
