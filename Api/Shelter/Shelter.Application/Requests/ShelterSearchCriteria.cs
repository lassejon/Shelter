namespace Shelter.Application.Requests;

public record ShelterSearchCriteria(
    double? MinLatitude,
    double? MaxLatitude,
    double? MinLongitude,
    double? MaxLongitude,
    int? Limit);

