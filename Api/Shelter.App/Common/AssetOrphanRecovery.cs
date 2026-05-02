using App.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.Common;

public sealed class AssetOrphanRecovery(IShelterDbContext db)
{
    public async Task<IReadOnlyList<string>> QueueOrphansFromDeletedPicturesAsync(
        IEnumerable<Guid> assetIdsBeingFreed,
        IEnumerable<Guid>? deletedShelterPictureIds = null,
        IEnumerable<Guid>? deletedReviewPictureIds = null,
        CancellationToken cancellationToken = default)
    {
        var assetIds = assetIdsBeingFreed.Distinct().ToList();
        if (assetIds.Count == 0) return [];

        var excludeShelterPics = (deletedShelterPictureIds ?? []).ToHashSet();
        var excludeReviewPics = (deletedReviewPictureIds ?? []).ToHashSet();

        var blobKeys = new List<string>();

        foreach (var assetId in assetIds)
        {
            var stillReferenced = await db.Shelters
                .AnyAsync(
                    s => s.Pictures.Any(p =>
                        p.AssetId == assetId && !excludeShelterPics.Contains(p.Id)),
                    cancellationToken);

            if (!stillReferenced)
            {
                stillReferenced = await db.Reviews
                    .AnyAsync(
                        r => r.Pictures.Any(p =>
                            p.AssetId == assetId && !excludeReviewPics.Contains(p.Id)),
                        cancellationToken);
            }

            if (stillReferenced) continue;

            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
            if (asset is null) continue;

            blobKeys.Add(asset.BlobKey);
            db.Assets.Remove(asset);
        }

        return blobKeys;
    }
}
