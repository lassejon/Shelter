using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;
using Shelter.Domain.Common;

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

        // MinRating: only include shelters that have at least one review and meet the average-rating threshold.
        if (request.MinRating.HasValue)
        {
            var minRating = request.MinRating.Value;
            if (minRating < 1 || minRating > 5)
                throw new DomainValidationException("MinRating must be between 1 and 5.");

            query = query.Where(s =>
                db.Reviews.Any(r => r.ShelterId == s.Id) &&
                db.Reviews.Where(r => r.ShelterId == s.Id).Average(r => (double)(int)r.Rating) >= minRating);
        }

        // Date availability: when both StartUtc and EndUtc are set, exclude shelters with any overlapping
        // non-cancelled booking. Standard interval-overlap test: existing.Start < requested.End && existing.End > requested.Start.
        if (request is { StartUtc: not null, EndUtc: not null })
        {
            if (request.EndUtc.Value <= request.StartUtc.Value)
                throw new DomainValidationException("EndUtc must be after StartUtc.");

            var startUtc = request.StartUtc.Value;
            var endUtc = request.EndUtc.Value;

            query = query.Where(s => !db.Bookings.Any(b =>
                b.ShelterId == s.Id &&
                b.Status != BookingStatus.Cancelled &&
                b.StartUtc < endUtc &&
                b.EndUtc > startUtc));
        }

        query = query.OrderBy(s => s.Name);

        if (request.Limit is > 0)
            query = query.Take(request.Limit.Value);

        var shelters = await query
            .Include(s => s.Pictures)
                .ThenInclude(p => p.Asset)
            .ToListAsync(cancellationToken);

        var summaries = await BuildSummariesAsync(shelters.Select(s => s.Id).ToList(), cancellationToken);

        return shelters
            .Select(s => SearchShelterResponse.FromDomain(s, storage, summaries.GetValueOrDefault(s.Id, ReviewSummary.Empty)))
            .ToList();
    }

    private async Task<Dictionary<Guid, ReviewSummary>> BuildSummariesAsync(
        IReadOnlyList<Guid> shelterIds,
        CancellationToken cancellationToken)
    {
        if (shelterIds.Count == 0) return new Dictionary<Guid, ReviewSummary>();

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
