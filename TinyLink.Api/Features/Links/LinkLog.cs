namespace TinyLink.Api.Features.Links;

internal static partial class LinkLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Created short link {ShortCode}")]
    public static partial void Created(ILogger logger, string shortCode);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Soft-deleted short link {ShortCode}")]
    public static partial void Deleted(ILogger logger, string shortCode);
}
