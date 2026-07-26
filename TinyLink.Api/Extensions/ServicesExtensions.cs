using TinyLink.Api.Services;

namespace TinyLink.Api.Extensions;

public static class ServicesExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddServices()
        {
            builder.Services.AddScoped<ShortenService>();

            builder.Services.AddProblemDetails();
            builder.Services.AddControllers();

            return builder;
        }
    }
}

