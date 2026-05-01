namespace App.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
