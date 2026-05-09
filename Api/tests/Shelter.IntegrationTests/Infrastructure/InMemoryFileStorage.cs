using System.Collections.Concurrent;
using App.Common;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Dictionary-backed <see cref="IFileStorage"/>. Avoids needing Azurite during tests and
/// guarantees that <c>BlobContainerClient</c> (which would otherwise try to connect on first
/// resolve) is never instantiated.
/// </summary>
public sealed class InMemoryFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    public async Task UploadAsync(string blobKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        _blobs[blobKey] = ms.ToArray();
    }

    public Task DeleteAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(blobKey, out _);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string blobKey) => $"https://test.local/{blobKey}";
}
