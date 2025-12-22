using System;
using System.Threading;
using System.Threading.Tasks;

using GEC.InspectionService.Data.States;

namespace GEC.InspectionService.Services.Abstractions;

public interface IGasInspectionService
{
    Task<GasInspectionState> CompleteGasInspectionAsync(Guid id, decimal co, decimal co2, decimal o2, int hc, int no, decimal lambda, DateTimeOffset completedAt, CancellationToken cancellationToken);

    Task<GasInspectionState> StartGasInspectionAsync(Guid id, string registrationNumber, DateTimeOffset startedAt, CancellationToken cancellationToken);
}
