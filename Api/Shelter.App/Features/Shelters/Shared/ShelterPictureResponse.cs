namespace App.Features.Shelters.Shared;

public record ShelterPictureResponse(
    Guid Id,
    string Url,
    string? Caption,
    int SortOrder);
