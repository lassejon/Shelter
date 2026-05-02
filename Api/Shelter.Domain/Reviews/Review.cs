using Shelter.Domain.Common;

namespace Shelter.Domain.Reviews;

public class Review
{
    private readonly List<ReviewPicture> _pictures = [];

    private Review() { }

    public Guid Id { get; private set; }
    public Guid ShelterId { get; private set; }
    public Guid ReviewerId { get; private set; }

    public Rating Rating { get; private set; }
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<ReviewPicture> Pictures => _pictures;

    public static Review Create(
        Guid shelterId,
        Guid reviewerId,
        Rating rating,
        string? comment,
        DateTimeOffset now)
    {
        ValidateRating(rating);

        return new Review
        {
            Id = Guid.NewGuid(),
            ShelterId = shelterId,
            ReviewerId = reviewerId,
            Rating = rating,
            Comment = comment,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Edit(Rating rating, string? comment, DateTimeOffset now)
    {
        ValidateRating(rating);

        Rating = rating;
        Comment = comment;
        UpdatedAt = now;
    }

    public ReviewPicture AddPicture(Guid assetId, string? caption, DateTimeOffset now)
    {
        if (assetId == Guid.Empty)
            throw new DomainValidationException("Picture asset id must be provided.");

        var picture = new ReviewPicture(Guid.NewGuid(), Id, assetId, caption, _pictures.Count);
        _pictures.Add(picture);
        UpdatedAt = now;
        return picture;
    }

    public void RemovePicture(Guid pictureId, DateTimeOffset now)
    {
        var picture = _pictures.FirstOrDefault(p => p.Id == pictureId);
        if (picture is null) return;

        _pictures.Remove(picture);
        UpdatedAt = now;
    }

    public bool WrittenBy(Guid userId) => ReviewerId == userId;

    private static void ValidateRating(Rating rating)
    {
        if (!Enum.IsDefined(rating))
            throw new DomainValidationException("Unknown rating.");
    }
}
