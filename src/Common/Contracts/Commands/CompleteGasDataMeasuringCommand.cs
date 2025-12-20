namespace GasEmissionsCheck.Common.Contracts.Commands;

public class CompleteGasDataMeasuringCommand
{
    public Guid CorrelationId { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    public decimal CO { get; set; }

    public decimal CO2 { get; set; }

    public decimal O2 { get; set; }

    public int HC { get; set; }

    public int NO { get; set; }

    public decimal Lambda { get; set; }
}
