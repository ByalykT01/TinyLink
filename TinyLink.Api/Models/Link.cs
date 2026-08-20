namespace TinyLink.Api.Models;

public class Link
{
    public long Id { get; init; }
    public required string ShortCode { get; init; }
    public required Uri TargetUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? DeletedAt { get; set; }
}
