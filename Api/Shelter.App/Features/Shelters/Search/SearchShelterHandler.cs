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
    /// Trigram similarity floor for matching Q against the shelter Name (symmetric similarity,
    /// short-vs-short). Below this, a name match is considered noise. pg_trgm's default is 0.3;
    /// 0.2 is more forgiving for typos.
    /// </summary>
    private const double NameSimilarityThreshold = 0.1;

    /// <summary>
    /// Word-similarity floor for matching Q against the shelter Description (asymmetric, short-vs-long).
    /// `word_similarity` scores higher than `similarity` because it measures the best matching
    /// word-bounded substring rather than overall trigram overlap, so the threshold is set higher
    /// to keep description hits relevant. pg_trgm's default is 0.6.
    /// </summary>
    private const double DescriptionSimilarityThreshold = 0.25;

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
            // Match on Name (symmetric trigram similarity) OR Description (asymmetric word similarity).
            // Description-only hits surface for feature queries ("water", "fireplace") that aren't in the
            // name; ranking below puts name matches first regardless. word_similarity treats the description
            // as a haystack and finds the best word-bounded substring vs the query, so it works on long text
            // where plain similarity() would dilute to ~0.
            query = query.Where(s =>
                TextFunctions.TrigramSimilarity(s.Name, trimmedQ!) >= NameSimilarityThreshold ||
                (s.Description != null &&
                    TextFunctions.TrigramWordSimilarity(trimmedQ!, s.Description) >= DescriptionSimilarityThreshold));
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

        // Ranking: name similarity dominates so a "Birch Hut" query lists the shelter named Birch Hut
        // above shelters that merely mention birch in passing. Description similarity is the secondary
        // sort so ties on name break by description relevance.
        query = hasNameQuery
            ? query.OrderByDescending(s => TextFunctions.TrigramSimilarity(s.Name, trimmedQ!))
                   .ThenByDescending(s => s.Description != null
                       ? TextFunctions.TrigramWordSimilarity(trimmedQ!, s.Description)
                       : 0d)
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
            .Select(b => new { b.ShelterId, Period = new BookingPeriod(b.StartUtc, b.EndUtc, b.Guests, b.Type) })
            .ToListAsync(cancellationToken);

        var periodsByShelter = bookings
            .GroupBy(b => b.ShelterId)
            .ToDictionary(g => g.Key, g => g.Select(b => b.Period).ToList());

        return shelters
            .Where(s =>
            {
                if (!periodsByShelter.TryGetValue(s.Id, out var periods))
                    return s.Capacity >= requestedGuests;

                // Any overlapping exclusive booking blocks the shelter, regardless of capacity.
                if (periods.Any(b => b.Type == BookingType.Exclusive)) return false;

                var peak = ShelterEntity.PeakInclusiveGuests(periods, startUtc, endUtc);
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
