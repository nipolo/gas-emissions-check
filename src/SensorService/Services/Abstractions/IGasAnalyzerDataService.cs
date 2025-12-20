using GasEmissionsCheck.SensorService.Domain;

namespace GasEmissionsCheck.SensorService.Services.Abstractions;

public interface IGasAnalyzerDataService
{
    bool TryParseData(byte[] gasAnalyzerDataBytes, out GasAnalyzerData data);
}