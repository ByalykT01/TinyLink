using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
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
}

