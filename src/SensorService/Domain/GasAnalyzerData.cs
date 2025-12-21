namespace GEC.SensorService.Domain;

public class GasAnalyzerData
{
    public decimal CO { get; set; }

    public decimal CO2 { get; set; }

    public decimal O2 { get; set; }

    public int HC { get; set; }

    public int NO { get; set; }

    public decimal Lambda
    {
        get
        {
            var a = 1.7261m / 4m;

            var top =
                CO2 + (CO / 2m) + O2
                + (((a * (3.5m / (3.5m + (CO / CO2)))) - 0.0088m) * (CO2 + CO));

            var bottom =
                (1m + a - 0.0088m) * (CO2 + CO + (6m * (HC / 10000m)));

            return bottom == 0 ? 0m : top / bottom;
        }
    }
}
