namespace GasEmissionsCheck.SensorService.Module;

public class DomainSettings
{
    public const string Key = nameof(DomainSettings);

    public decimal COStartThreshold { get; set; }
}
