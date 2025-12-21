using GEC.SensorService.Domain;
using GEC.SensorService.Services.Abstractions;

namespace GEC.SensorService.Services;

public class GasAnalyzerDataService : IGasAnalyzerDataService
{
    public bool TryParseData(byte[] gasAnalyzerDataBytes, out GasAnalyzerData data)
    {
        data = null;

        if (gasAnalyzerDataBytes.Length != 43)
        {
            return false;
        }

        if (!(gasAnalyzerDataBytes[0] == 6
              && gasAnalyzerDataBytes[1] == 49
              && gasAnalyzerDataBytes[2] == 82
              && gasAnalyzerDataBytes[3] == 71))
        {
            return false;
        }

        // Layout:
        // 0..4 header (5)
        // 5..11 CO (7)
        // 12..17 CO2 (6)
        // 18..23 HC (6)
        // 24..29 O2 (6)
        // 30..35 NO (6)

        if (!decimal.TryParse(SliceAsString(gasAnalyzerDataBytes, 5, 7), out var co))
        {
            return false;
        }

        if (!decimal.TryParse(SliceAsString(gasAnalyzerDataBytes, 12, 6), out var co2))
        {
            return false;
        }

        if (!int.TryParse(SliceAsString(gasAnalyzerDataBytes, 18, 6), out var hc))
        {
            return false;
        }

        if (!decimal.TryParse(SliceAsString(gasAnalyzerDataBytes, 24, 6), out var o2))
        {
            return false;
        }

        if (!int.TryParse(SliceAsString(gasAnalyzerDataBytes, 30, 6), out var no))
        {
            return false;
        }

        data = new GasAnalyzerData { CO = co, CO2 = co2, HC = hc, NO = no, O2 = o2 };

        return true;
    }

    private static string SliceAsString(byte[] bytes, int startIndex, int count)
    {
        var chars = new char[count];
        for (var index = 0; index < count; index++)
        {
            chars[index] = (char)bytes[startIndex + index];
        }

        return new string(chars).Trim();
    }
}
