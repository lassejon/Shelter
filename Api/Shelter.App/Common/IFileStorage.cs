namespace App.Common;

public interface IFileStorage
{
    Task UploadAsync(
        string blobKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default);

    string GetPublicUrl(string blobKey);
}
