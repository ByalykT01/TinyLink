using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class RateLimitingEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Post_ExceedsLimit_Returns429WithRetryAfter()
    {
        await using var app = fixture.CreateApplicationWithRateLimit(
            burst: 2,
            perMinute: 2);
        using var client = app.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        for (var index = 0; index < 2; index++)
        {
            var accepted = await client.PostAsJsonAsync(
                "/api/links",
                new { url = $"https://example.com/{index}" });
            accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        var rejected = await client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/rejected" });
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        rejected.Headers.RetryAfter.Should().NotBeNull();
        rejected.Headers.RetryAfter!.Delta.Should().BeGreaterThan(TimeSpan.Zero);
    }
}

