using Microsoft.EntityFrameworkCore;

namespace GettingFeaturesFromDatabase.Database;

public class SqliteDbContext : DbContext
{
    public DbSet<Feature> Features { get; set; } = null!;
    
    public SqliteDbContext(DbContextOptions<SqliteDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feature>().HasKey(x => x.Name);
    }
}