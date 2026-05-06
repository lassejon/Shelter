namespace App.Persistence;

public static class TextFunctions
{
    /// <summary>
    /// Trigram similarity between two strings (0..1). Provider-mapped to Postgres'
    /// <c>pg_trgm</c> <c>similarity(a, b)</c> in
    /// <c>ShelterDbContext.OnModelCreating</c>. Use only inside LINQ queries against
    /// <see cref="IShelterDbContext"/>; throws if invoked at runtime.
    /// </summary>
    public static double TrigramSimilarity(string a, string b) => throw new InvalidOperationException(
        $"{nameof(TrigramSimilarity)} can only be used inside a LINQ query against IShelterDbContext.");
}
