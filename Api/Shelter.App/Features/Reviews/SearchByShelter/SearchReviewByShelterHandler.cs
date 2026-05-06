using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Reviews.SearchByShelter;

public sealed class SearchReviewByShelterHandler(IShelterDbContext db, IFileStorage storage)
{
    private const int MaxPageSize = 100;

    public async Task<SearchReviewByShelterResponse> HandleAsync(
        Guid shelterId,
        PaginationParameters paging,
        CancellationToken cancellationToken)
    {
        var shelterExists = await db.Shelters
            .AnyAsync(s => s.Id == shelterId, cancellationToken);
        if (!shelterExists)
            throw new DomainNotFoundException($"Shelter {shelterId} was not found.");

        var page = paging.Page ?? 1;
        if (page < 1) page = 1;

        var pageSize = paging.PageSize ?? 10;
        if (pageSize is < 1 or > MaxPageSize) pageSize = 10;

        var baseQuery = db.Reviews.AsNoTracking().Where(r => r.ShelterId == shelterId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var avgRating = totalCount == 0
            ? 0d
            : await baseQuery.AverageAsync(r => (double)(int)r.Rating, cancellationToken);
        var summary = totalCount == 0
            ? ReviewSummary.Empty
            : new ReviewSummary(Math.Round(avgRating, 2), totalCount);

        var reviews = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Reviewer)
            .Include(r => r.Pictures)
                .ThenInclude(p => p.Asset)
            .ToListAsync(cancellationToken);

        return new SearchReviewByShelterResponse(
            reviews.Select(r => ReviewDetailResponse.FromDomain(r, storage)).ToList(),
            summary,
            Pagination.From(page, pageSize, totalCount));
    }
}
