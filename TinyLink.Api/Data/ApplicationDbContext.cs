using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TinyLink.Api.Models;

class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var propertyCreatedAt = entity.FindProperty("CreatedAt");
            if (propertyCreatedAt != null && propertyCreatedAt.ClrType == typeof(DateTime))
            {
                propertyCreatedAt.SetDefaultValueSql("timezone('utc', now())");
                propertyCreatedAt.ValueGenerated = ValueGenerated.OnAdd;
            }
        }

    }

    public DbSet<Link> Links => Set<Link>();
}
