using TinyLink.Api.Options;

namespace TinyLink.Api.Extensions;

public static class OptionsExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddAppOptions()
        {
            builder.Services.AddOptions<DatabaseOptions>()
                .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName)).ValidateDataAnnotations()
                .ValidateOnStart();
            return builder;
        }
    }
}

