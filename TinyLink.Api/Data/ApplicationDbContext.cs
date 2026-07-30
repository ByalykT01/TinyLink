using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Models;
using TinyLink.Api.ShortCodes;

namespace TinyLink.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Link>(entity =>
        {
            entity.Property(l => l.Id).ValueGeneratedNever();
            entity.Property(l => l.ShortCode).HasMaxLength(7).IsRequired();
            entity.HasIndex(l => l.ShortCode).IsUnique();
            entity.Property(l => l.TargetUrl).HasMaxLength(2048).IsRequired();
        });

        modelBuilder.HasSequence<long>("link_code_req")
            .StartsAt(1).IncrementsBy(1)
            .HasMin(1).HasMax(Base62.Domain - 1) // keyed Feistel max num of values
            .IsCyclic(false);

        // foreach (var entity in modelBuilder.Model.GetEntityTypes())
        // {
        //     var propertyCreatedAt = entity.FindProperty("CreatedAt");
        //     if (propertyCreatedAt != null && propertyCreatedAt.ClrType == typeof(DateTimeOffset))
        //     {
        //         propertyCreatedAt.SetDefaultValueSql("now()");
        //         propertyCreatedAt.ValueGenerated = ValueGenerated.OnAdd;
        //     }
        // }

    }

    public DbSet<Link> Links => Set<Link>();
}
