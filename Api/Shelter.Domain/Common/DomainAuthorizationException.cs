namespace Shelter.Domain.Common;

public sealed class DomainAuthorizationException(string message) : DomainException(message);
