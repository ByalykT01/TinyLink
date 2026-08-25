using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;
namespace TinyLink.Tests.Integration.Api;

[CollectionDefinition(Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}

[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "xUnit owns the fixture lifetime and calls IAsyncLifetime.DisposeAsync.")]
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine").Build();
    private TinyLinkApp? _app;
    public HttpClient Client { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _app = new TinyLinkApp(_postgres.GetConnectionString());
        Client = _app.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        await _postgres.DisposeAsync();
    }
    private sealed class TinyLinkApp(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var connection = new NpgsqlConnectionStringBuilder(connectionString);
            builder.UseEnvironment("Development");
            builder.UseSetting("Database:Host", connection.Host!);
            builder.UseSetting("Database:Port", connection.Port.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting("Database:Name", connection.Database!);
            builder.UseSetting("Database:User", connection.Username!);
            builder.UseSetting("Database:Password", connection.Password!);
            builder.UseSetting("ShortCodes:Key", Convert.ToBase64String(new byte[32]));
            builder.UseSetting("RateLimiting:CreateLink:Burst", "1000");
            builder.UseSetting("RateLimiting:CreateLink:PerMinute", "1000");
        }
    }
}
