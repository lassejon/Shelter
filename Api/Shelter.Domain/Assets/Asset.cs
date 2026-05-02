using Shelter.Domain.Common;

namespace Shelter.Domain.Assets;

public class Asset
{
    private Asset() { }

    public Guid Id { get; private set; }
    public Guid UploadedById { get; private set; }

    public string BlobKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Asset Create(
        Guid uploadedById,
        string blobKey,
        string contentType,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(blobKey))
            throw new DomainValidationException("Asset blob key must be provided.");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new DomainValidationException("Asset content type must be provided.");

        return new Asset
        {
            Id = Guid.NewGuid(),
            UploadedById = uploadedById,
            BlobKey = blobKey,
            ContentType = contentType,
            CreatedAt = now,
        };
    }
}
