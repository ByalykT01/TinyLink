using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using System.Text.Json;

namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class CreateLinkEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Post_ValidUrl_Returns201WithShortCodeAndLocation()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/links",
            new { url = "https://example.com/hello" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        body!.ShortCode.Should().HaveLength(7);
        response.Headers.Location!.OriginalString.Should().Be($"/{body.ShortCode}");
    }
    private sealed record CreatedLink(string ShortCode, DateTimeOffset? ExpiresAt);

    [Fact]
    public async Task Post_InvalidUrl_Returns400WithValidationProblem()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "not-a-url" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors")
            .TryGetProperty("url", out _)
            .Should().BeTrue();
    }
}

