using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Data;
using TinyLink.Api.Features.Links;
using TinyLink.Api.Models;
using Xunit;

namespace TinyLink.Tests.Integration.Data;

[Collection(PostgresCollectionDefinition.Name)]
public sealed class DeletedLinkCleanupTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _retention = TimeSpan.FromDays(7);
    private static readonly DateTimeOffset _cutoff = _now - _retention;

    public async Task InitializeAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        await dbContext.Links.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExecuteAsync_RemovesLinksDeletedBeforeRetention()
    {
        await SeedAsync(NewLink(id: 1, deletedAt: _cutoff.AddSeconds(-1)));
        var cleanup = CreateCleanup();

        var removed = await cleanup.ExecuteAsync(_retention, CancellationToken.None);

        removed.Should().Be(1);
        await AssertRemainingAsync();
    }

    [Fact]
    public async Task ExecuteAsync_RemovesLinksDeletedExactlyAtCutoff()
    {
        await SeedAsync(NewLink(id: 1, deletedAt: _cutoff));
        var cleanup = CreateCleanup();

        var removed = await cleanup.ExecuteAsync(_retention, CancellationToken.None);

        removed.Should().Be(1);
        await AssertRemainingAsync();
    }

    [Fact]
    public async Task ExecuteAsync_KeepsLinksDeletedAfterCutoff()
    {
        await SeedAsync(NewLink(id: 1, deletedAt: _cutoff.AddHours(1)));
        var cleanup = CreateCleanup();

        var removed = await cleanup.ExecuteAsync(_retention, CancellationToken.None);

        removed.Should().Be(0);
        await AssertRemainingAsync(1);
    }

    [Fact]
    public async Task ExecuteAsync_KeepsActiveLinks()
    {
        await SeedAsync(NewLink(id: 1, expiresAt: _now.AddDays(7)));
        var cleanup = CreateCleanup();

        var removed = await cleanup.ExecuteAsync(_retention, CancellationToken.None);

        removed.Should().Be(0);
        await AssertRemainingAsync(1);
    }

    [Fact]
    public async Task ExecuteAsync_RemovesLinksExpiredBeforeRetention()
    {
        await SeedAsync(NewLink(id: 1, expiresAt: _cutoff.AddHours(-1)));
        var cleanup = CreateCleanup();

        var removed = await cleanup.ExecuteAsync(_retention, CancellationToken.None);

        removed.Should().Be(1);
        await AssertRemainingAsync();
    }

    [Fact]
    public async Task ExecuteAsync_SecondRunRemovesNothing()
    {
        await SeedAsync(NewLink(id: 1, deletedAt: _cutoff.AddDays(-1)));
        var cleanup = CreateCleanup();
        var ct = CancellationToken.None;

        var first = await cleanup.ExecuteAsync(_retention, ct);
        var second = await cleanup.ExecuteAsync(_retention, ct);

        first.Should().Be(1);
        second.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentRunsDeleteEachRowOnceInTotal()
    {
        await SeedAsync(
            NewLink(id: 1, deletedAt: _cutoff.AddDays(-1)),
            NewLink(id: 2, deletedAt: _cutoff.AddDays(-2)));
        var ct = CancellationToken.None;

        var results = await Task.WhenAll(
            CreateCleanup().ExecuteAsync(_retention, ct),
            CreateCleanup().ExecuteAsync(_retention, ct));

        results.Sum().Should().Be(2);
        await AssertRemainingAsync();
    }

    private DeletedLinkCleanup CreateCleanup() =>
        new(new FuncDbContextFactory(fixture.CreateDbContext), new FixedTimeProvider(_now));

    private async Task SeedAsync(params Link[] links)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Links.AddRange(links);
        await dbContext.SaveChangesAsync();
    }

    private async Task AssertRemainingAsync(params long[] expectedIds)
    {
        await using var dbContext = fixture.CreateDbContext();
        var remaining = await dbContext.Links
            .OrderBy(link => link.Id)
            .Select(link => link.Id)
            .ToListAsync();
        remaining.Should().Equal(expectedIds);
    }

    private static Link NewLink(
        long id,
        DateTimeOffset? deletedAt = null,
        DateTimeOffset? expiresAt = null) => new()
        {
            Id = id,
            ShortCode = $"T{id:000000}",
            TargetUrl = new Uri("https://example.com/"),
            CreatedAt = _cutoff.AddDays(-1),
            ExpiresAt = expiresAt,
            DeletedAt = deletedAt
        };

    private sealed class FuncDbContextFactory(Func<ApplicationDbContext> create)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => create();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
