using Microsoft.EntityFrameworkCore;

namespace GloboTicket.Catalog.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasKey(e => e.EventId);
        modelBuilder.Entity<Event>().Property(e => e.Name).HasMaxLength(200);
        modelBuilder.Entity<Event>().Property(e => e.Artist).HasMaxLength(200);
    }
}
