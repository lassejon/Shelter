using App.Common;
using App.Features.Reviews.Shared;
using App.Features.Shelters.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Shelters.Get;

public sealed class GetShelterHandler(IShelterDbContext db, IFileStorage storage)
{
    public async Task<ShelterDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var shelter = await db.Shelters
            .Include(s => s.Pictures)
                .ThenInclude(p => p.Asset)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new DomainNotFoundException($"Shelter {id} was not found.");

        var summary = await LoadSummaryAsync(id, cancellationToken);

        return ShelterDetailResponse.FromDomain(shelter, storage, summary);
    }

    private async Task<ReviewSummary> LoadSummaryAsync(Guid shelterId, CancellationToken cancellationToken)
    {
        var row = await db.Reviews
            .AsNoTracking()
            .Where(r => r.ShelterId == shelterId)
            .GroupBy(r => r.ShelterId)
            .Select(g => new { Average = g.Average(r => (double)(int)r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? ReviewSummary.Empty
            : new ReviewSummary(Math.Round(row.Average, 2), row.Count);
    }
}
