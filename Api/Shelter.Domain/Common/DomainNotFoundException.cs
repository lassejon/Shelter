namespace Shelter.Domain.Common;

public sealed class DomainNotFoundException(string message) : DomainException(message);
