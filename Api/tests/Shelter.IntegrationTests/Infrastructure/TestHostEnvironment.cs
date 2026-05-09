using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Shelter.IntegrationTests.Infrastructure;

/// <summary>
/// Stub <see cref="IHostEnvironment"/> for the Tier 2 DI graph (no real host is built). Marked
/// "Testing" so the dev-only branches in <c>AddInfrastructure</c> (sensitive-data logging,
/// detailed errors) are skipped — we want production-shaped behaviour with a test database.
/// </summary>
internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "Shelter.IntegrationTests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
