using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TinyLink.Api.Features.Links;
using Xunit;
namespace TinyLink.Tests.Integration.Api;

[Collection(ApiCollectionDefinition.Name)]
public sealed class DeleteLinkEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Delete_ValidToken_Returns204AndRedirectBecomes410()
    {
        var created = await CreateLinkAsync("valid-delete");
        using var beforeDelete =
            await fixture.Client.GetAsync(new Uri($"/{created.ShortCode}"));
        beforeDelete.StatusCode.Should().Be(HttpStatusCode.Found);
        using var deleted =
            await DeleteAsync(created.ShortCode, created.DeleteToken);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var afterDelete =
            await fixture.Client.GetAsync(new Uri($"/{created.ShortCode}"));
        afterDelete.StatusCode.Should().Be(HttpStatusCode.Gone);
        afterDelete.Headers.CacheControl!.Public.Should().BeTrue();
        afterDelete.Headers.CacheControl.MaxAge
            .Should().Be(TimeSpan.FromDays(1));
    }
    [Fact]
    public async Task Delete_MissingToken_Returns404()
    {
        var created = await CreateLinkAsync("missing-token");
        using var response = await fixture.Client.DeleteAsync(
            new Uri($"/api/links/{created.ShortCode}"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Delete_WrongToken_Returns404AndLinkRemainsActive()
    {
        var created = await CreateLinkAsync("wrong-token");
        var wrongToken = DeleteToken.Generate().Value;
        using var response =
            await DeleteAsync(created.ShortCode, wrongToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var redirect =
            await fixture.Client.GetAsync(new Uri($"/{created.ShortCode}"));
        redirect.StatusCode.Should().Be(HttpStatusCode.Found);
    }
    [Fact]
    public async Task Delete_WrongAuthorizationScheme_Returns404()
    {
        var created = await CreateLinkAsync("wrong-scheme");
        using var response = await DeleteAsync(
            created.ShortCode,
            created.DeleteToken,
            scheme: "Basic");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Delete_UnknownCode_Returns404()
    {
        var token = DeleteToken.Generate().Value;
        using var response = await DeleteAsync(
            "zzzzzzz",
            token);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Delete_AlreadyDeletedLink_Returns204()
    {
        var created = await CreateLinkAsync("repeated-delete");
        using var first =
            await DeleteAsync(created.ShortCode, created.DeleteToken);
        using var second =
            await DeleteAsync(created.ShortCode, created.DeleteToken);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    private async Task<CreatedLink> CreateLinkAsync(string suffix)
    {
        using var response = await fixture.Client.PostAsJsonAsync(
            "/api/links",
            new { url = $"https://example.com/{suffix}" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedLink>();
        body.Should().NotBeNull();
        body.DeleteToken.Should().NotBeNullOrWhiteSpace();
        return body;
    }
    private async Task<HttpResponseMessage> DeleteAsync(
        string code,
        string token,
        string scheme = "Bearer")
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/links/{code}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(scheme, token);
        return await fixture.Client.SendAsync(request);
    }
    private sealed record CreatedLink(
        string ShortCode,
        string DeleteToken);
}

