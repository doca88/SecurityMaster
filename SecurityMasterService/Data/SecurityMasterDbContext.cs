using Microsoft.EntityFrameworkCore;

namespace SecurityMasterService.Data;

public class SecurityMasterDbContext : DbContext
{
    public SecurityMasterDbContext(DbContextOptions<SecurityMasterDbContext> options)
        : base(options) { }

    public DbSet<Security> Securities => Set<Security>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Allocation> Allocations => Set<Allocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecurityConfiguration).Assembly);
    }
}