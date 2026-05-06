using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Bookings;
using Shelter.Domain.Common;
using ShelterEntity = Shelter.Domain.Shelters.Shelter;

namespace App.Features.Shelters.Search;

public sealed class SearchShelterHandler(IShelterDbContext db, IFileStorage storage)
{
    /// <summary>
    /// Trigram similarity floor for the free-text Q filter. Below this the match is considered noise
    /// and the shelter is dropped. Tuned empirically; revisit if logs show too many false positives /
    /// negatives. pg_trgm's default is 0.3.
    /// </summary>
    private const double NameSimilarityThreshold = 0.2;

    public async Task<IReadOnlyList<SearchShelterResponse>> HandleAsync(
        SearchShelterRequest request,
        CancellationToken cancellationToken)
    {
        var query = db.Shelters
            .AsNoTracking()
            .Where(s => s.IsActive);

        var trimmedQ = request.Q?.Trim();
        var hasNameQuery = !string.IsNullOrEmpty(trimmedQ);
        if (hasNameQuery)
        {
            // Postgres pg_trgm: similarity(name, q) > threshold; ranking handled below.
            query = query.Where(s =>
                TextFunctions.TrigramSimilarity(s.Name, trimmedQ!) >= NameSimilarityThreshold);
        }

        if (request is { MinLatitude: not null, MaxLatitude: not null, MinLongitude: not null, MaxLongitude: not null })
        {
            query = query.Where(s =>
                s.Latitude  >= request.MinLatitude.Value &&
                s.Latitude  <= request.MaxLatitude.Value &&
                s.Longitude >= request.MinLongitude.Value &&
                s.Longitude <= request.MaxLongitude.Value);
        }

        // Capacity floor: the explicit MinCapacity filter, raised by Guests if larger. Guests beyond total
        // capacity can never fit, so this prefilter saves the per-shelter availability work below.
        var capacityFloor = Math.Max(request.MinCapacity ?? 0, request.Guests ?? 0);
        if (capacityFloor > 0)
            query = query.Where(s => s.Capacity >= capacityFloor);

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

        query = hasNameQuery
            ? query.OrderByDescending(s => TextFunctions.TrigramSimilarity(s.Name, trimmedQ!))
                   .ThenBy(s => s.Name)
            : query.OrderBy(s => s.Name);

        if (request.Limit is > 0)
            query = query.Take(request.Limit.Value);

        var shelters = await query
            .Include(s => s.Pictures)
                .ThenInclude(p => p.Asset)
            .ToListAsync(cancellationToken);

        // Date availability: when both StartUtc and EndUtc are set, drop shelters that don't have remaining
        // capacity for the requested party at every moment in [StartUtc, EndUtc). Done in-memory because the
        // sweepline (peak concurrent inclusive guests + exclusive overlap blocks) is awkward to express in SQL.
        if (request is { StartUtc: not null, EndUtc: not null })
        {
            if (request.EndUtc.Value <= request.StartUtc.Value)
                throw new DomainValidationException("EndUtc must be after StartUtc.");

            shelters = await FilterByAvailabilityAsync(
                shelters,
                request.StartUtc.Value,
                request.EndUtc.Value,
                request.Guests ?? 1,
                cancellationToken);
        }

        var summaries = await BuildSummariesAsync(shelters.Select(s => s.Id).ToList(), cancellationToken);

        return shelters
            .Select(s => SearchShelterResponse.FromDomain(s, storage, summaries.GetValueOrDefault(s.Id, ReviewSummary.Empty)))
            .ToList();
    }

    private async Task<List<ShelterEntity>> FilterByAvailabilityAsync(
        List<ShelterEntity> shelters,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        int requestedGuests,
        CancellationToken cancellationToken)
    {
        if (shelters.Count == 0) return shelters;

        var shelterIds = shelters.Select(s => s.Id).ToList();
        var bookings = await db.Bookings
            .AsNoTracking()
            .Where(b =>
                shelterIds.Contains(b.ShelterId) &&
                b.Status != BookingStatus.Cancelled &&
                b.StartUtc < endUtc &&
                b.EndUtc > startUtc)
            .Select(b => new { b.ShelterId, b.StartUtc, b.EndUtc, b.Guests, b.Type })
            .ToListAsync(cancellationToken);

        var bookingsByShelter = bookings.ToLookup(b => b.ShelterId);

        return shelters
            .Where(s =>
            {
                var shelterBookings = bookingsByShelter[s.Id].ToList();
                // Any overlapping exclusive booking blocks the entire shelter for the window.
                if (shelterBookings.Any(b => b.Type == BookingType.Exclusive)) return false;

                // Inclusive bookings share capacity. Peak concurrent inclusive guests must leave room for the
                // requested party. Sweepline: peak can only occur at a candidate moment in {startUtc} ∪ {b.Start
                // for b in overlapping bookings, clamped to the requested window}.
                if (shelterBookings.Count == 0) return s.Capacity >= requestedGuests;

                var candidates = new List<DateTimeOffset> { startUtc };
                candidates.AddRange(shelterBookings
                    .Select(b => b.StartUtc)
                    .Where(t => t > startUtc && t < endUtc));

                var peak = candidates.Max(t => shelterBookings
                    .Where(b => b.StartUtc <= t && t < b.EndUtc)
                    .Sum(b => b.Guests));

                return s.Capacity - peak >= requestedGuests;
            })
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
