using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TinyLink.Api.Data;
using TinyLink.Api.Features.Links;
using TinyLink.Api.Models;
using Xunit;

namespace TinyLink.Tests.Integration.Data;

[CollectionDefinition(Name)]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine").Build();
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;
        return new ApplicationDbContext(options);
    }
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
[Collection(PostgresCollectionDefinition.Name)]
public sealed class ApplicationDbContextTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);
    public async Task InitializeAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        await dbContext.Links.ExecuteDeleteAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;
    [Fact]
    public async Task Link_RoundTripsEveryProperty()
    {
        var link = NewLink(shortCode: "aZ09");
        await using (var write = fixture.CreateDbContext())
        {
            write.Links.Add(link);
            await write.SaveChangesAsync();
        }
        await using var read = fixture.CreateDbContext();
        var persisted = await read.Links.SingleAsync(l => l.Id == link.Id);
        persisted.Should().BeEquivalentTo(link);
    }
    [Fact]
    public async Task ShortCode_RejectsDuplicates()
    {
        await using (var seed = fixture.CreateDbContext())
        {
            seed.Links.Add(NewLink(id: 1, shortCode: "collide"));
            await seed.SaveChangesAsync();
        }
        await using var conflicting = fixture.CreateDbContext();
        conflicting.Links.Add(NewLink(id: 2, shortCode: "collide"));
        var act = () => conflicting.SaveChangesAsync();
        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        thrown.WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
    [Fact]
    public async Task TargetUrl_IsRequired()
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Links.Add(NewLink(targetUrl: null));
        var act = () => dbContext.SaveChangesAsync();
        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        thrown.WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.NotNullViolation);
    }
    [Fact]
    public async Task TargetUrl_IsCappedAtTheLengthUrlPolicyAllows()
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Links.Add(NewLink(
            targetUrl: $"https://example.com/{new string('a', UrlPolicy.MaxLength)}"));
        var act = () => dbContext.SaveChangesAsync();
        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        thrown.WithInnerException<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation);
    }
    [Fact]
    public async Task Timestamps_ComeBackAsUtc()
    {
        await using (var write = fixture.CreateDbContext())
        {
            write.Links.Add(NewLink());
            await write.SaveChangesAsync();
        }
        await using var read = fixture.CreateDbContext();
        var persisted = await read.Links.SingleAsync();
        persisted.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        persisted.ExpiresAt.Should().NotBeNull();
        persisted.ExpiresAt.Value.Offset.Should().Be(TimeSpan.Zero);
    }
    [Fact]
    public async Task ExpiresAt_WithNonUtcOffset_IsRejectedByTheProvider()
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Links.Add(NewLink(
            expiresAt: new DateTimeOffset(2026, 8, 5, 19, 0, 0, TimeSpan.FromHours(2))));
        var act = () => dbContext.SaveChangesAsync();
        var thrown = await act.Should().ThrowAsync<Exception>(
            "Npgsql 6+ rejects non-UTC DateTimeOffset for timestamptz, " +
            "so the handler must normalise client input to UTC before persisting");
        thrown.Which.GetBaseException().Should().BeOfType<ArgumentException>()
            .Which.Message.Should().Contain("only offset 0 (UTC) is supported");
    }
    private static Link NewLink(
        long id = 1,
        string shortCode = "abc",
        string? targetUrl = "https://example.com/",
        DateTimeOffset? expiresAt = null) => new()
        {
            Id = id,
            ShortCode = shortCode,
            TargetUrl = targetUrl is null ? null! : new Uri(targetUrl),
            CreatedAt = _now,
            ExpiresAt = expiresAt ?? _now.AddDays(7)
        };
}
