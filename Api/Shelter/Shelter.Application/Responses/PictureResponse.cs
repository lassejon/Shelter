using Shelter.Domain.Pictures;

namespace Shelter.Application.Responses;

public record PictureResponse(
    Guid Id,
    string Url,
    string? Caption,
    int SortOrder)
{
    public static PictureResponse FromDomain(Picture picture) => new(
        picture.Id,
        picture.Url,
        picture.Caption,
        picture.SortOrder);
}

