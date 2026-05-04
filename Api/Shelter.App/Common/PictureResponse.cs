namespace App.Common;

public record PictureResponse(
    Guid Id,
    string Url,
    string? Caption,
    int SortOrder);
