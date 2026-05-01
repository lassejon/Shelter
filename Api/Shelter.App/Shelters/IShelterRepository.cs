using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Shelters;

public interface IShelterRepository
{
    Task<ShelterEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ShelterEntity shelter, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
