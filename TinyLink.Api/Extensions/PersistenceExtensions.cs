using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TinyLink.Api.Data;
using TinyLink.Api.Options;

namespace TinyLink.Api.Extensions;

public static class PersistenceExtensions
{
    public static IHostApplicationBuilder AddPersistence(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<ApplicationDbContext>((services, options) =>
            options.UseNpgsql(services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString));

        builder.Services.AddHealthChecks()
            .AddNpgSql(
                services => services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ToConnectionString,
                name: "postgres",
                timeout: TimeSpan.FromSeconds(3));

        return builder;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
