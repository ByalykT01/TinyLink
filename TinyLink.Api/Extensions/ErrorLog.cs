namespace TinyLink.Api.Extensions;

internal static partial class ErrorLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Client disconnected during {Path}")]
    public static partial void ClientDisconnected(ILogger logger, PathString path);
    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Unique constraint {Constraint} violated")]
    public static partial void UniqueViolation(ILogger logger, string constraint);
    [LoggerMessage(EventId = 2003, Level = LogLevel.Critical,
        Message = "Short code sequence exhausted")]
    public static partial void SequenceExhausted(ILogger logger);
    [LoggerMessage(EventId = 2004, Level = LogLevel.Error,
        Message = "Database unavailable")]
    public static partial void DatabaseUnavailable(ILogger logger, Exception exception);
}

