using App.Common;
using App.Features.Reviews.Shared;
using App.Features.Shelters.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.SearchByOwner;

/// <summary>
/// Lists every shelter owned by <paramref name="ownerId"/> — including deactivated ones —
/// for the owner's management surface. Unlike <c>SearchShelterHandler</c> (public bbox/text
/// search of active shelters), this returns the full set so the owner can reactivate or edit.
/// </summary>
public sealed class SearchShelterByOwnerHandler(IShelterDbContext db, IFileStorage storage)
{
    public async Task<IReadOnlyList<ShelterDetailResponse>> HandleAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var shelters = await db.Shelters
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .Include(s => s.Pictures)
                .ThenInclude(p => p.Asset)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);

        if (shelters.Count == 0) return [];

        var summaries = await BuildSummariesAsync(
            shelters.Select(s => s.Id).ToList(),
            cancellationToken);

        return shelters
            .Select(s => ShelterDetailResponse.FromDomain(
                s, storage, summaries.GetValueOrDefault(s.Id, ReviewSummary.Empty)))
            .ToList();
    }
 
    private async Task<Dictionary<Guid, ReviewSummary>> BuildSummariesAsync(
        IReadOnlyList<Guid> shelterIds,
        CancellationToken cancellationToken)
    {
        var rows = await db.Reviews
            .AsNoTracking()
            .Where(r => shelterIds.Contains(r.ShelterId))
            .GroupBy(r => r.ShelterId)
            .Select(g => new { ShelterId = g.Key, Average = g.Average(r => (double)(int)r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            x => x.ShelterId,
            x => new ReviewSummary(Math.Round(x.Average, 2), x.Count));
    }
}
