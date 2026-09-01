using TinyLink.Api.Options;

namespace TinyLink.Api.Extensions;

public static class OptionsExtensions
{

    public static IHostApplicationBuilder AddOptions(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<DatabaseOptions>()
            .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<LinkCleanupOptions>()
            .Bind(builder.Configuration.GetSection(LinkCleanupOptions.SectionName))
            .Validate(
                options => options.Interval > TimeSpan.Zero,
                "Link cleanup interval must be greated than zero")
            .Validate(
                options => options.Retention > TimeSpan.Zero,
                "Link cleanup retention must be greated than zero")
            .ValidateOnStart();

        return builder;
    }
}
