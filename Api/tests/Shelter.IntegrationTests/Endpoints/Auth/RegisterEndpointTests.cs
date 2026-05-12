using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shelter.IntegrationTests.Infrastructure;

namespace Shelter.IntegrationTests.Endpoints.Auth;

/// <summary>
/// The shape <c>{ status: 400, ..., errors: string[] }</c> is the contract the frontend's
/// register form depends on (it iterates <c>errors</c> to display password-policy violations).
/// No Tier 1 or Tier 2 test would catch a regression in this shape — it's the composition of
/// <c>ProblemDetails.Extensions["errors"]</c>, <c>JsonExtensionData</c> serialization, and the
/// endpoint's <c>TypedResults.BadRequest(ProblemDetails)</c> return path. This is the test the
/// strat doc singles out as "first in" when Tier 3 lands.
/// </summary>
public sealed class RegisterEndpointTests(PostgresFixture postgres) : ApiTestBase(postgres)
{
    [Fact]
    public async Task Invalid_password_returns_400_with_errors_string_array()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "shortpw@test.local",
            password = "abc",
            firstName = "Test",
            lastName = "User",
            isShelterOwner = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetInt32().Should().Be(400);

        var errors = json.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);
        errors.GetArrayLength().Should().BeGreaterThan(0);

        foreach (var err in errors.EnumerateArray())
            err.ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task Happy_path_returns_200_with_token()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "newuser@test.local",
            password = "ValidPass1!",
            firstName = "New",
            lastName = "User",
            isShelterOwner = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("email").GetString().Should().Be("newuser@test.local");
    }
}
