using GEC.InspectionService.Data.States;

using Microsoft.EntityFrameworkCore;

namespace GEC.InspectionService.Data.Adapter;

public partial class InspectionServiceDBContext : DbContext
{
    public InspectionServiceDBContext()
    {
    }

    public InspectionServiceDBContext(DbContextOptions<InspectionServiceDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<GasInspectionState> GasInspections { get; set; }

    public void EnsureDBMigrated()
    {
        Database.Migrate();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SetupGasInspectionsTable(modelBuilder);
    }

    private static void SetupGasInspectionsTable(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GasInspectionState>(entity =>
        {
            entity.ToTable("GasInspections");

            entity.HasKey(e => e.Id);
        });
    }
}
