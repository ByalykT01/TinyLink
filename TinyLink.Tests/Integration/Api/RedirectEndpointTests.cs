using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class RedirectEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Get_KnownCode_Returns302WithTargetAndNoStore()
    {
        const string target = "https://example.com/redirect-me";
        var created = await fixture.Client.PostAsJsonAsync("/api/links", new { url = target });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var response = await fixture.Client.GetAsync(created.Headers.Location);
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.AbsoluteUri.Should().Be(target);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }
    [Fact]
    public async Task Get_UnknownCode_Returns404()
    {
        var response = await fixture.Client.GetAsync(new Uri("/zzzzzzz"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

