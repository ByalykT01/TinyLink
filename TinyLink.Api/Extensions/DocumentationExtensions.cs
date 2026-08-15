using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace TinyLink.Api.Extensions;

public static class DocumentationExtensions
{
    public static IHostApplicationBuilder AddDocumentation(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(o =>
        {
            o.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

            o.AddDocumentTransformer((document, context, token) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "TinyLink API",
                    Description = "TinyLink - ASP.NET project focused on presenting the development skills on a simple idea - URL shortener API.",
                    Version = "v1"
                };
                return Task.CompletedTask;
            });
        });
        return builder;
    }

    public static IApplicationBuilder UseScalarWithDefaults(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.MapScalarApiReference();
        }
        return app;
    }
}
