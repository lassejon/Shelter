using App.Common;
using App.Features.Reviews.Shared;
using App.Persistence;
using Microsoft.EntityFrameworkCore;
using Shelter.Domain.Common;

namespace App.Features.Reviews.GetMine;

public sealed class GetMyReviewHandler(IShelterDbContext db, IFileStorage storage)
{
    public async Task<ReviewDetailResponse> HandleAsync(
        Guid shelterId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var review = await db.Reviews
            .AsNoTracking()
            .Include(r => r.Reviewer)
            .Include(r => r.Pictures)
                .ThenInclude(p => p.Asset)
            .FirstOrDefaultAsync(r => r.ShelterId == shelterId && r.ReviewerId == userId, cancellationToken)
            ?? throw new DomainNotFoundException(
                $"You have not reviewed shelter {shelterId}.");

        return ReviewDetailResponse.FromDomain(review, storage);
    }
}
