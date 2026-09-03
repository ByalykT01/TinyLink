using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class ProblemDetailsTests(ApiFixture fixture)
{
    [Fact]
    public async Task ErrorResponse_IsProblemJsonCarryingTraceId()
    {
        var response = await fixture.Client.GetAsync(new Uri("/zzzzzzz", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("traceId", out var traceId)
            .Should().BeTrue("AddErrorHandling supplies the CustomizeProblemDetails callback that emits it");
        traceId.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidationError_IsProblemJsonCarryingTraceId()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "not-a-url" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RateLimitedResponse_IsProblemJsonCarryingTraceId()
    {
        await using var app = fixture.CreateApplicationWithRateLimit(
            burst: 1,
            perMinute: 1);
        using var client = app.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        var accepted = await client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/problem-details" });
        accepted.StatusCode.Should().Be(HttpStatusCode.Created);
        var rejected = await client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/problem-details-rejected" });
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        problem.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrEmpty();
    }
}
