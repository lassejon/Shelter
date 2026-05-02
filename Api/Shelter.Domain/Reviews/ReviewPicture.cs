using Shelter.Domain.Assets;

namespace Shelter.Domain.Reviews;

public class ReviewPicture
{
    private ReviewPicture() { }

    internal ReviewPicture(Guid id, Guid reviewId, Guid assetId, string? caption, int sortOrder)
    {
        Id = id;
        ReviewId = reviewId;
        AssetId = assetId;
        Caption = caption;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid ReviewId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public string? Caption { get; private set; }
    public int SortOrder { get; private set; }
}
