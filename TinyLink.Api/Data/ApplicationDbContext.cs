using Microsoft.EntityFrameworkCore;
using TinyLink.Api.Features.Links;
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
            entity.Property(l => l.ShortCode).HasMaxLength(Base62.CodeLength).IsRequired();
            entity.HasIndex(l => l.ShortCode).IsUnique();
            entity.Property(l => l.TargetUrl)
                .HasConversion(v => v.AbsoluteUri, v => new Uri(v))
                .HasMaxLength(UrlPolicy.MaxLength)
                .IsRequired();
        });

        modelBuilder.HasSequence<long>("link_code_req")
            .StartsAt(1).IncrementsBy(1)
            .HasMin(1).HasMax(Base62.Domain - 1) // keyed Feistel max num of values
            .IsCyclic(false);
    }

    public DbSet<Link> Links => Set<Link>();
}
