using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Options;

namespace TinyLink.Api.Extensions;

public static class PersistenceExtensions
{
    public static IHostApplicationBuilder AddPersistence(this IHostApplicationBuilder builder)
    {
        var database = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ??
                       throw new InvalidOperationException("Database Configuration not found");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(database.ToConnectionString));

        return builder;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
