using System.Diagnostics.CodeAnalysis;

namespace TinyLink.Api.Models;

public class Link
{
    public long Id { get; init; }
    public required string ShortCode { get; init; }
    public required Uri TargetUrl { get; init; }

    [SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Justification = "EF Core maps byte arrays directly to PostgreSQL bytea columns.")]
    public byte[]? DeleteTokenHash { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
}
