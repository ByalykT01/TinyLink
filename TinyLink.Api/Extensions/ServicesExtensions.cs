using TinyLink.Api.Data;

namespace TinyLink.Api.Extensions;

public static class ServicesExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddServices()
        {

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddScoped<ShortCodeAllocator>();

            builder.Services.AddProblemDetails();
            builder.Services.AddControllers();

            return builder;
        }
    }
}

