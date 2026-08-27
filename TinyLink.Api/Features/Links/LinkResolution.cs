namespace TinyLink.Api.Features.Links;

public sealed record LinkResolution(
    bool Exists,
    Uri? TargetUrl,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? DeletedAt
        )
{
    public static LinkResolution NotFound { get; } =
        new(false, null, null, null);
}


