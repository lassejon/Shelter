using Shelter.Domain.Pictures;

namespace Shelter.Application.Requests;

public record PictureRequest(
    string Url,
    string? Caption,
    int SortOrder)
{
    public Picture ToDomain(Guid ownerId, Guid shelterId) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = ownerId,
        Scope = PictureScope.Shelter,
        Url = Url,
        Caption = Caption,
        SortOrder = SortOrder,
        ShelterId = shelterId
    };
}

