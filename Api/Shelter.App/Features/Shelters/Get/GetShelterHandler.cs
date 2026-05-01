using App.Features.Shelters.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Get;

public sealed class GetShelterHandler(IShelterDbContext db)
{
    public async Task<ShelterDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .Include(s => s.Pictures)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {id} was not found.");

        return ShelterDetailResponse.FromDomain(shelter);
    }
}
