namespace App.Common;

/// <summary>
/// Frontend deployment URLs the API needs to reference (e.g. when building outgoing email
/// links). Populated by Infrastructure's <c>FrontendSettings</c> binder; injected directly
/// into handlers that need to construct user-facing URLs.
/// </summary>
public sealed record FrontendUrls(string BaseUrl);
