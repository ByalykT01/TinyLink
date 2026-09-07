using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Models;
using Xunit;

namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class GetLinkStatusEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Get_KnownCode_Returns200WithTargetAndExpiry()
    {
        const string target = "https://example.com/status-me";
        var created = await fixture.Client.PostAsJsonAsync("/api/links", new { url = target });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<CreatedLink>();
        createdBody.Should().NotBeNull();

        var response = await fixture.Client.GetAsync(new Uri($"/api/links/{createdBody!.ShortCode}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<LinkStatus>();
        body.Should().NotBeNull();
        body!.TargetUrl.Should().Be(target);
        body.ExpiresAt.Should().BeCloseTo(
            createdBody.ExpiresAt!.Value,
            precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Get_UnknownCode_Returns404()
    {
        var response = await fixture.Client.GetAsync(new Uri("/api/links/zzzzzzz", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_DeletedCode_Returns410()
    {
        var created = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/status-deleted" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        await fixture.ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Links
                .Where(link => link.ShortCode == body!.ShortCode)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(link => link.DeletedAt, DateTimeOffset.UtcNow));
        });

        var response = await fixture.Client.GetAsync(new Uri($"/api/links/{body!.ShortCode}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task Get_ExpiredCode_Returns410()
    {
        var created = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/status-expired" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        await fixture.ExecuteDbContextAsync(async dbContext =>
        {
            await dbContext.Links
                .Where(link => link.ShortCode == body!.ShortCode)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(
                        link => link.ExpiresAt,
                        DateTimeOffset.UtcNow.AddMinutes(-1)));
        });

        var response = await fixture.Client.GetAsync(new Uri($"/api/links/{body!.ShortCode}", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    private sealed record CreatedLink(
        string ShortCode,
        DateTimeOffset? ExpiresAt);

    private sealed record LinkStatus(
        string TargetUrl,
        DateTimeOffset? ExpiresAt);
}
