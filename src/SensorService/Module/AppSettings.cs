namespace GasEmissionsCheck.SensorService.Module;

public class AppSettings
{
    public const string Key = nameof(AppSettings);

    public string GasAnalyzerCOMPort { get; set; }

    public string LogFilePath { get; set; }
}
