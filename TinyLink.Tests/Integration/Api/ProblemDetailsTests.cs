using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
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
}

