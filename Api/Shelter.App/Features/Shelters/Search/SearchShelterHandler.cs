using App.Common;
using App.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.Features.Shelters.Search;

public sealed class SearchShelterHandler(IShelterDbContext db, IFileStorage storage)
{
    public async Task<IReadOnlyList<SearchShelterResponse>> HandleAsync(
        SearchShelterRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Shelters
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (request is { MinLatitude: not null, MaxLatitude: not null, MinLongitude: not null, MaxLongitude: not null })
        {
            query = query.Where(s =>
                s.Latitude  >= request.MinLatitude.Value &&
                s.Latitude  <= request.MaxLatitude.Value &&
                s.Longitude >= request.MinLongitude.Value &&
                s.Longitude <= request.MaxLongitude.Value);
        }

        if (request.MinCapacity.HasValue)
            query = query.Where(s => s.Capacity >= request.MinCapacity.Value);

        if (request.MaxCapacity.HasValue)
            query = query.Where(s => s.Capacity <= request.MaxCapacity.Value);

        // MinRating filter is deferred until the Reviews aggregate exists.

        query = query.OrderBy(s => s.Name);

        if (request.Limit is > 0)
            query = query.Take(request.Limit.Value);

        var shelters = await query
            .Include(s => s.Pictures)
                .ThenInclude(p => p.Asset)
            .ToListAsync(cancellationToken);

        return shelters.Select(s => SearchShelterResponse.FromDomain(s, storage)).ToList();
    }
}
