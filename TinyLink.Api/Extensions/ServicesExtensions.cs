using TinyLink.Api.Data;
using TinyLink.Api.Features.Links;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Extensions;

public static class ServicesExtensions
{

    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        var cipherKey = builder.Configuration["ShortCodes:Key"]
?? throw new InvalidOperationException("ShortCodes:Key is not configured.");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(new Cipher(Convert.FromBase64String(cipherKey)));
        builder.Services.AddSingleton<UrlPolicy>();
        builder.Services.AddScoped<ShortCodeAllocator>();

        return builder;
    }
}

