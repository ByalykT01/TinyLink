using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TinyLink.Api.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        var otelResource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: "TinyLink.Api");
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
                    {
                        tracing.SetResourceBuilder(otelResource)
                        .AddAspNetCoreInstrumentation(options =>
                                {
                                    options.Filter = httpContext =>
                                    {
                                        var path = httpContext.Request.Path.Value ?? string.Empty;
                                        return !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
                                    };
                                })
                        .AddHttpClientInstrumentation()
                        .AddNpgsql()
                        .AddSource("TinyLink.Api")
                        .AddOtlpExporter();
                    })
        .WithMetrics(metrics =>
                {
                    metrics.SetResourceBuilder(otelResource)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddMeter("TinyLink.Api")
                        .AddOtlpExporter();
                });


        return builder;
    }
}
