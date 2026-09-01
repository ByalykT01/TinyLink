namespace TinyLink.Api.Options;

public sealed class LinkCleanupOptions
{
    public const string SectionName = "LinkCleanup";

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    public TimeSpan Retention { get; init; } = TimeSpan.FromDays(7);
}
