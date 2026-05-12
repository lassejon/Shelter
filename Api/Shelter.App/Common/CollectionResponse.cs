namespace App.Common;

public sealed record CollectionResponse<T>(IReadOnlyList<T> Items);
