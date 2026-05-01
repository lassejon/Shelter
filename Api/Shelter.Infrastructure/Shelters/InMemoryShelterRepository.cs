using System.Collections.Concurrent;
using App.Shelters;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace Shelter.Infrastructure.Shelters;

internal sealed class InMemoryShelterRepository : IShelterRepository
{
    private readonly ConcurrentDictionary<Guid, ShelterEntity> _shelters = new();

    public Task<ShelterEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _shelters.TryGetValue(id, out var shelter);
        return Task.FromResult(shelter);
    }

    public Task AddAsync(ShelterEntity shelter, CancellationToken cancellationToken)
    {
        if (!_shelters.TryAdd(shelter.Id, shelter))
            throw new InvalidOperationException($"A shelter with id {shelter.Id} already exists.");
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
