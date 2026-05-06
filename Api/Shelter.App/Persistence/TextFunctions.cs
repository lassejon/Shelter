namespace App.Persistence;

public static class TextFunctions
{
    /// <summary>
    /// Symmetric trigram similarity between two strings (0..1). Best for short-vs-short matching
    /// (e.g. shelter name vs query). Provider-mapped to Postgres' <c>pg_trgm</c>
    /// <c>similarity(a, b)</c> in <c>ShelterDbContext.OnModelCreating</c>. Use only inside LINQ
    /// queries against <see cref="IShelterDbContext"/>; throws if invoked at runtime.
    /// </summary>
    public static double TrigramSimilarity(string a, string b) => throw new InvalidOperationException(
        $"{nameof(TrigramSimilarity)} can only be used inside a LINQ query against IShelterDbContext.");

    /// <summary>
    /// Asymmetric trigram word similarity (0..1). Returns the highest similarity for any
    /// word-bounded substring of <paramref name="haystack"/> against the entire
    /// <paramref name="needle"/>. Best for short-vs-long matching (e.g. query vs shelter
    /// description). Provider-mapped to Postgres' <c>pg_trgm</c>
    /// <c>word_similarity(needle, haystack)</c> in <c>ShelterDbContext.OnModelCreating</c>.
    /// Use only inside LINQ queries against <see cref="IShelterDbContext"/>; throws if invoked at runtime.
    /// </summary>
    public static double TrigramWordSimilarity(string needle, string haystack) => throw new InvalidOperationException(
        $"{nameof(TrigramWordSimilarity)} can only be used inside a LINQ query against IShelterDbContext.");
}
