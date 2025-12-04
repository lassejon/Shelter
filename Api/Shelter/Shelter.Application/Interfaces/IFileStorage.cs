namespace Shelter.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveShelterPictureAsync(Guid shelterId, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteShelterPictureAsync(string urlOrKey, CancellationToken ct = default);
}