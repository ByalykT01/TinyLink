using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Models;
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
    public async Task Get_UnknownCode_Returns404WithNoStore()
    {
        var response = await fixture.Client.GetAsync(new Uri("/zzzzzzz"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();

    }

    [Fact]
    public async Task Get_WellFormedFabricatedCode_Returns404()
    {
        var response = await fixture.Client.GetAsync(new Uri("/ABC1234"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Fact]
    public async Task Get_UnknownCodeIsNotCached_CreatedAfterwardsRedirects()
    {
        const string code = "NEG0001";
        using var beforeInsert = await fixture.Client.GetAsync(new Uri($"/{code}"));
        beforeInsert.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await fixture.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Links.Add(new Link
            {
                Id = 990_001,
                ShortCode = code,
                TargetUrl = new Uri("https://example.com/negative-cache"),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            });
            await dbContext.SaveChangesAsync();
        });
        using var afterInsert = await fixture.Client.GetAsync(new Uri($"/{code}"));
        afterInsert.StatusCode.Should().Be(HttpStatusCode.Found);
        afterInsert.Headers.Location!.AbsoluteUri.Should().Be("https://example.com/negative-cache");
    }

    [Fact]
    public async Task Get_ExpiredCode_Returns410WithOneDayCache()
    {
        var created = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = "https://example.com/expired" });
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
        var response = await fixture.Client.GetAsync(new Uri($"/{body!.ShortCode}"));
        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromDays(1));
    }

    private sealed record CreatedLink(
    string ShortCode,
    DateTimeOffset? ExpiresAt);

}

