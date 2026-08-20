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
        return builder;
    }
}
