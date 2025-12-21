using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using GEC.InspectionService.Data.Adapter;
using GEC.InspectionService.Infrastructure.IPCamera.Module;
using GEC.InspectionService.Infrastructure.IPCamera.Services;
using GEC.InspectionService.Infrastructure.IPCamera.Services.Abstractions;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Module;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Services;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Services.Abstractions;
using GEC.InspectionService.Module;
using GEC.InspectionService.Services;
using GEC.InspectionService.Services.Abstractions;
using GEC.InspectionService.Workers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace GEC.InspectionService;

public class Program
{
    private static IConfigurationRoot s_config;

    private static async Task Main(string[] args)
    {
        s_config = BuildConfiguration();

        var app = SetupApplication(args);

        app.Run();
    }

    private static IHost SetupApplication(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        SetupLogging(builder);

        AddDataModule(builder.Services, s_config);

        AddServices(builder.Services);

        AddSettings(builder.Services);

        builder.Services.AddHostedService<ConsumersHostedService>();

        var app = builder.Build();

        ApplicationPostSetup(app);

        return app;
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var os = Environment.OSVersion.Platform == PlatformID.Unix ? "Linux" : "Windows";

        return new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile($"appsettings.{os}.json", optional: false)
                    .AddEnvironmentVariables().Build();
    }

    private static void SetupLogging(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();

        builder.Logging.AddConfiguration(builder.Configuration);

        builder.Logging.AddConsole();
    }

    private static IServiceCollection AddDataModule(IServiceCollection services, IConfiguration configuration)
    {
        var dbConnectionString = configuration.GetConnectionString(DBConsts.DB_NAME);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(dbConnectionString);

        dataSourceBuilder.EnableDynamicJson();

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContextFactory<InspectionServiceDBContext>(options => options.UseNpgsql(dataSource));

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.TryAddSingleton<ICameraSnapshotService, TapoCameraSnapshotService>();
        services.TryAddSingleton<IPlateRecognizerAdapterService, PlateRecognizerAdapterService>();
        services.TryAddSingleton<IGasInspectionService, GasInspectionService>();

        services.AddHttpClient(HttpClientConsts.PlateRecognizer, (sp, client) =>
        {
            var plateRecognizerOptions = sp.GetRequiredService<IOptions<PlateRecognizerOptions>>().Value;

            client.BaseAddress = new Uri(plateRecognizerOptions.BaseUrl);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", plateRecognizerOptions.ApiToken);

            client.Timeout = TimeSpan.FromSeconds(plateRecognizerOptions.TimeoutSeconds);
        });
    }

    private static void AddSettings(IServiceCollection services)
    {
        services.Configure<RabbitMQSettings>(s_config.GetSection(RabbitMQSettings.Key));
        services.Configure<TPLinkTapoSnapshotOptions>(s_config.GetSection(TPLinkTapoSnapshotOptions.Key));
        services.Configure<PlateRecognizerOptions>(s_config.GetSection(PlateRecognizerOptions.Key));
    }

    private static void ApplicationPostSetup(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InspectionServiceDBContext>();

        context.EnsureDBMigrated();
    }
}
