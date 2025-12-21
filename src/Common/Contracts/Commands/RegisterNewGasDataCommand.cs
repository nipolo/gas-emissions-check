namespace GEC.Common.Contracts.Commands;

public class RegisterNewGasDataCommand
{
    public Guid CorrelationId { get; set; }

    public DateTimeOffset StartedAt { get; set; }
}
