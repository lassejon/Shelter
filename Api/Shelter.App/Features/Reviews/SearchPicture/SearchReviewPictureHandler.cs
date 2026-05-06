using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Reviews.SearchPicture;

public sealed class SearchReviewPictureHandler(IShelterDbContext db, IFileStorage storage)
{
    private const int MaxPageSize = 100;

    public async Task<SearchReviewPictureResponse> HandleAsync(
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

        var baseQuery = db.Reviews
            .AsNoTracking()
            .Where(r => r.ShelterId == shelterId)
            .SelectMany(r => r.Pictures);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .OrderByDescending(p => p.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Asset.BlobKey,
                p.Caption,
                p.SortOrder,
            })
            .ToListAsync(cancellationToken);

        var pictures = rows
            .Select(r => new PictureResponse(r.Id, storage.GetPublicUrl(r.BlobKey), r.Caption, r.SortOrder))
            .ToList();

        return new SearchReviewPictureResponse(
            pictures,
            Pagination.From(page, pageSize, totalCount));
    }
}
