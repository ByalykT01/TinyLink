using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TinyLink.Api.Extensions;

public static class ObservabilityExtensions
{
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        var configuredName = builder.Configuration["OTEL_SERVICE_NAME"];
        var serviceName = string.IsNullOrWhiteSpace(configuredName)
            ? builder.Environment.ApplicationName
            : configuredName;

        var otelResource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName);
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
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddSource("TinyLink.Api")
                        .AddOtlpExporter();
                    })
        .WithMetrics(metrics =>
                {
                    metrics.SetResourceBuilder(otelResource)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter("TinyLink.Api")
                        .AddOtlpExporter();
                });

        builder.Logging.AddOpenTelemetry(logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.AddOtlpExporter();
                });


        return builder;
    }
}
