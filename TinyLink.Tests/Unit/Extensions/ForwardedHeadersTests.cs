using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TinyLink.Api.Extensions;
using Xunit;

namespace TinyLink.Tests.Unit.Extensions;

public sealed class ForwardedHeadersTests
{
    [Theory]
    [InlineData(null, "203.0.113.10", false)]
    [InlineData(null, "127.0.0.1", true)]
    [InlineData("192.0.2.0/24", "192.0.2.10", true)]
    [InlineData("192.0.2.0/24", "203.0.113.10", false)]
    [InlineData("192.0.2.0/24", "127.0.0.1", false)]
    public async Task ForwardedHeaders_AreAcceptedOnlyFromTrustedPeers(
        string? network, string peer, bool trusted)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        if (network is not null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:KnownNetworks:0"] = network
            });
        }
        builder.AddForwardedHeaders();
        using var provider = builder.Services.BuildServiceProvider();
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>());
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.20";
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(context);

        context.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse(trusted ? "198.51.100.20" : peer));
        context.Request.Scheme.Should().Be(trusted ? "https" : "http");
    }
}
