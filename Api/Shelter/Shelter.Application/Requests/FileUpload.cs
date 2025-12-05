namespace Shelter.Application.Requests;

public record FileUpload(
    Stream Content,
    string ContentType,
    string FileName);

