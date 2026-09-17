using Microsoft.AspNetCore.HttpOverrides;
namespace TinyLink.Api.Extensions;

public static class ForwardedHeadersExtensions
{
    public static IHostApplicationBuilder AddForwardedHeaders(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            var networks = builder.Configuration
                .GetSection("ForwardedHeaders:KnownNetworks")
                .Get<string[]>() ?? [];

            // Keep the framework's loopback-only defaults when no proxies are configured.
            // Empty trust lists would accept forwarded headers from every sender.
            if (networks.Length == 0)
                return;

#pragma warning disable ASPDEPR005
            options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var cidr in networks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
            }
        });
        return builder;
    }
    public static WebApplication UseForwardedHeadersFromConfig(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseForwardedHeaders();
        return app;
    }
}
