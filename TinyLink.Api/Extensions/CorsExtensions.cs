namespace TinyLink.Api.Extensions;

/// <summary>Named CORS policy for the browser frontend (no cookies involved).</summary>
public static class CorsExtensions
{
    public const string FrontendPolicy = "frontend";

    public static IHostApplicationBuilder AddFrontendCors(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var origins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>() ?? [];
        builder.Services.AddCors(options =>
            options.AddPolicy(FrontendPolicy, policy =>
            {
                // Bearer tokens travel in the Authorization header, not cookies,
                // so no AllowCredentials is needed (and it would forbid wildcards).
                policy.AllowAnyHeader().AllowAnyMethod();
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins);
                }
                else
                {
                    // No origins configured: same-origin + dev-proxy setups keep
                    // working, every cross-origin browser call is rejected.
                    policy.SetIsOriginAllowed(static _ => false);
                }
            }));
        return builder;
    }
}
