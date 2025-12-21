using GEC.SensorService.Domain;

namespace GEC.SensorService.Services.Abstractions;

public interface IGasAnalyzerDataService
{
    bool TryParseData(byte[] gasAnalyzerDataBytes, out GasAnalyzerData data);
}