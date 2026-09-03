using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Features.Links;
using Xunit;
namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class CreateLinkEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Post_ValidUrl_Returns201WithShortCodeLocationAndDeleteToken()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/hello" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        body!.ShortCode.Should().HaveLength(7);
        body.DeleteToken.Should().NotBeNullOrWhiteSpace();
        response.Headers.Location!.OriginalString
            .Should().Be($"/{body.ShortCode}");
        await fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var storedHash = await dbContext.Links
                .Where(link => link.ShortCode == body.ShortCode)
                .Select(link => link.DeleteTokenHash)
                .SingleAsync();
            storedHash.Should().NotBeNull();
            DeleteToken.Matches(body.DeleteToken, storedHash!)
                .Should().BeTrue();
        });
    }
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
    [Fact]
    public async Task Post_ExplicitExpiration_PersistsAsUtc()
    {
        var requested = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/explicit-expiry", expiresAt = requested });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        body!.ExpiresAt.Should().Be(requested.ToUniversalTime());
        body.ExpiresAt!.Value.Offset.Should().Be(TimeSpan.Zero);
        await fixture.ExecuteDbContextAsync(async dbContext =>
        {
            var stored = await dbContext.Links
                .Where(link => link.ShortCode == body.ShortCode)
                .Select(link => link.ExpiresAt)
                .SingleAsync();
            stored.Should().Be(requested.ToUniversalTime());
            stored!.Value.Offset.Should().Be(TimeSpan.Zero);
        });
    }
    [Fact]
    public async Task Post_PastExpiration_Returns400WithValidationProblem()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new
            {
                url = "https://example.com/past-expiry",
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors")
            .TryGetProperty("expiresAt", out _)
            .Should().BeTrue();
    }
    private sealed record CreatedLink(
        string ShortCode,
        DateTimeOffset? ExpiresAt,
        string DeleteToken);
}
