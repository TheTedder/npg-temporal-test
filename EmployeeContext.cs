using Microsoft.EntityFrameworkCore;

namespace NpgTemporalTest;

public class EmployeeContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            "Host=localhost;Username=admin;Password=password;Database=npgtest",
            o =>
            {
                o.SetPostgresVersion(18, 0);
            });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.Property(e => e.ValidPeriod)
                .HasDefaultValueSql("tsrange(now(), 'infinity', '[)')");
                
             e.HasKey(e => new { e.EmployeeId, e.ValidPeriod }).WithoutOverlaps();
        });

        //modelBuilder.Entity<Employee>().HasTemporalKey("EmployeeId", "ValidPeriod");
    }
}