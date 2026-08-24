using System.Diagnostics;
namespace TinyLink.Api.Extensions;

public static class ErrorHandlingExtensions
{
    public static IHostApplicationBuilder AddErrorHandling(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var includeExceptionDetail = builder.Environment.IsDevelopment();
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                var http = context.HttpContext;
                context.ProblemDetails.Instance ??= $"{http.Request.Method} {http.Request.Path}";
                context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
                if (includeExceptionDetail && context.Exception is { } ex)
                {
                    context.ProblemDetails.Extensions["exception"] =
                        new { type = ex.GetType().FullName, message = ex.Message };
                }
            });
        // First handler that returns true wins, so register narrow before broad.
        builder.Services.AddExceptionHandler<ClientDisconnectedExceptionHandler>();
        builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();
        return builder;
    }
    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}

