using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
namespace TinyLink.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string CreateLinkPolicy = "create-link";
    public static IServiceCollection AddTinyLinkRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var tokenLimit = configuration.GetValue("RateLimiting:CreateLink:Burst", 20);
        var perMinute = configuration.GetValue("RateLimiting:CreateLink:PerMinute", 20);
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(CreateLinkPolicy, http =>
                RateLimitPartition.GetTokenBucketLimiter(
                    ClientKey(http),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = tokenLimit,
                        TokensPerPeriod = perMinute,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }
                await Results.Problem(
                        title: "Too many requests.",
                        detail: "You have created too many links recently. Try again shortly.",
                        statusCode: StatusCodes.Status429TooManyRequests)
                    .ExecuteAsync(context.HttpContext);
            };
        });
    }
    private static string ClientKey(HttpContext http)
    {
        var address = http.Connection.RemoteIpAddress;
        if (address is null)
            return "unknown";
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (address.AddressFamily is AddressFamily.InterNetworkV6)
        {
            // A single subscriber usually holds an entire /64, so partitioning on
            // the full address would let them rotate through billions of buckets.
            var bytes = address.GetAddressBytes();
            Array.Clear(bytes, 8, 8);
            return new IPAddress(bytes).ToString();
        }
        return address.ToString();
    }
}

