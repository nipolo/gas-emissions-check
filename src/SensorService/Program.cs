using System;
using System.Threading.Tasks;

using GasEmissionsCheck.SensorService.Infrastructure;
using GasEmissionsCheck.SensorService.Infrastructure.Abstractions;
using GasEmissionsCheck.SensorService.Module;
using GasEmissionsCheck.SensorService.Services;
using GasEmissionsCheck.SensorService.Services.Abstractions;
using GasEmissionsCheck.SensorService.Workers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GasEmissionsCheck.SensorService;

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

        AddServices(builder.Services);

        AddSettings(builder.Services);

        builder.Services.AddHostedService<GasAnalyzerSensorWorker>();

        var app = builder.Build();

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

    private static void AddServices(IServiceCollection services)
    {
        services.TryAddSingleton<ICommandPublisher, CommandPublisherService>();

        services.TryAddSingleton<IGasAnalyzerDataService, GasAnalyzerDataService>();
    }

    private static void AddSettings(IServiceCollection services)
    {
        services.Configure<AppSettings>(s_config.GetSection(AppSettings.Key));
        services.Configure<DomainSettings>(s_config.GetSection(DomainSettings.Key));
        services.Configure<RabbitMQSettings>(s_config.GetSection(RabbitMQSettings.Key));
    }
}
