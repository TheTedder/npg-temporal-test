using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NpgsqlTypes;

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

    // TODO: Use a Savechanges interceptor instead.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries())
        {
            var prop = entry.Metadata.FindPrimaryKey().Properties[^1];
            
            // Hack! Use an annotation instead.
            if (prop.ClrType != typeof(NpgsqlRange<DateTime>))
                continue;

            NpgsqlRange<DateTime> period = (NpgsqlRange<DateTime>)entry.CurrentValues[prop];
            DateTime now = DateTime.UtcNow;

            switch (entry.State)
            {
                case EntityState.Modified:
                    // Update the record to be valid from here to infinity.
                    entry.CurrentValues[prop] = new NpgsqlRange<DateTime>(now, true, DateTime.MaxValue, false);
                    
                    // Construct an old version of the record to replace the one being modified.
                    object oldEntry = entry.Entity.GetType().GetConstructor(System.Type.EmptyTypes).Invoke(null);

                    // TODO: Get rid of this step and rewrite this whole function so that we're inserting instead of modifying.
                    var oldValues = await entry.GetDatabaseValuesAsync();

                    // Set the old version to retain its original starting period but end its validity at the current time.
                    oldValues[prop] = new NpgsqlRange<DateTime>(period.LowerBound, true, now, false);

                    // Copy the pre-modified values over.
                    foreach (var property in oldValues.Properties)
                    {
                        property.PropertyInfo.SetValue(oldEntry, oldValues[property]);
                    }

                    Add(oldEntry);
                    break;

                case EntityState.Deleted:
                    entry.CurrentValues[prop] = new NpgsqlRange<DateTime>(period.LowerBound, true, now, false);
                    break;
            }
        }

        return await base.SaveChangesAsync();
    }
}