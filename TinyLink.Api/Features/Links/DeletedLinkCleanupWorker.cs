using System.Data.Common;
using Microsoft.Extensions.Options;
using TinyLink.Api.Features.Links;
using TinyLink.Api.Options;

internal sealed class DeletedLinkCleanupWorker(
        DeletedLinkCleanup cleanup,
        TimeProvider clock,
        IOptions<LinkCleanupOptions> options,
        ILogger<DeletedLinkCleanupWorker> logger) : BackgroundService
{
    private async Task RunOnceAsync(
            TimeSpan retention,
            CancellationToken ct)
    {
        try
        {
            var deletedCount = await cleanup.ExecuteAsync(retention, ct);

            if (deletedCount > 0)
            {
                DeletedLinkCleanupLog.Completed(logger, deletedCount);
            }
        }
        catch (DbException exception)
        {
            DeletedLinkCleanupLog.Failed(logger, exception);
        }
    }

    protected override async Task ExecuteAsync(
        CancellationToken ct
            )
    {
        try
        {
            await RunOnceAsync(options.Value.Retention, ct);

            using var timer = new PeriodicTimer(options.Value.Interval, clock);

            while (await timer.WaitForNextTickAsync(ct))
            {
                await RunOnceAsync(options.Value.Retention, ct);
            }

        }
        catch (OperationCanceledException)
        when (ct.IsCancellationRequested)
        {

        }
    }
}

internal static partial class DeletedLinkCleanupLog
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Permanently deleted {DeletedLinkCount} links")]
    public static partial void Completed(
        ILogger logger,
        int deletedLinkCount);
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Error,
        Message = "Deleted-link cleanup failed")]
    public static partial void Failed(
        ILogger logger,
        Exception exception);
}
