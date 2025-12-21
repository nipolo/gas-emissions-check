using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Npgsql;

namespace GEC.InspectionService.Data.Adapter;

/// <summary>
/// Context factory used for managing EF DB Migrations
/// </summary>
public class InspectionServiceDBContextFactory : IDesignTimeDbContextFactory<InspectionServiceDBContext>
{
    public InspectionServiceDBContext CreateDbContext(string[] args)
    {
        // Used only for creating new migration
        var dataSourceBuilder = new NpgsqlDataSourceBuilder("Host=gec.inspection-service.dbserver;Port=5432;Database=InspectionServiceDB;Username=papa_joo;Password=1qaz!QAZ");

        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<InspectionServiceDBContext>();

        optionsBuilder.UseNpgsql(dataSource);

        return new InspectionServiceDBContext(optionsBuilder.Options);
    }
}
