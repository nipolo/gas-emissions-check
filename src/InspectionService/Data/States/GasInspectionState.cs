using System;

namespace GEC.InspectionService.Data.States;

public class GasInspectionState
{
    public Guid Id { get; set; }

    public string RegistrationNumber { get; set; }

    public DateTimeOffset StartedOn { get; set; }

    public DateTimeOffset? CompletedOn { get; set; }

    public decimal? CO { get; set; }

    public decimal? CO2 { get; set; }

    public decimal? O2 { get; set; }

    public int? HC { get; set; }

    public int? NO { get; set; }

    public decimal? Lambda { get; set; }
}
