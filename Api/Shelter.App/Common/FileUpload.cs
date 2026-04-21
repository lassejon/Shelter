namespace App.Common;

public record FileUpload(
    Stream Content,
    string ContentType,
    string FileName);
