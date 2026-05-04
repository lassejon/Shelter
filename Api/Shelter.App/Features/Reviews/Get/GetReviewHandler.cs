using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Reviews.Get;

public sealed class GetReviewHandler(IShelterDbContext db, IFileStorage storage)
{
    public async Task<ReviewDetailResponse> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var review = await db.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Pictures)
                .ThenInclude(p => p.Asset)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new DomainNotFoundException($"Review {id} was not found.");

        return ReviewDetailResponse.FromDomain(review, storage);
    }
}
