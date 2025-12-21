using System;
using System.Threading.Tasks;

using GEC.InspectionService.Data.Adapter;
using GEC.InspectionService.Data.States;
using GEC.InspectionService.Services.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace GEC.InspectionService.Services;

public class GasInspectionService : IGasInspectionService
{
    private readonly IDbContextFactory<InspectionServiceDBContext> _dbContextFactory;

    public GasInspectionService(IDbContextFactory<InspectionServiceDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<GasInspectionState> StartGasInspectionAsync(Guid id, string registerNumber, DateTimeOffset startedAt)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        var newGasInspection = new GasInspectionState
        {
            Id = id,
            RegistrationNumber = registerNumber,
            StartedOn = startedAt
        };

        await dbContext.GasInspections.AddAsync(newGasInspection);

        await dbContext.SaveChangesAsync();

        return newGasInspection;
    }

    public async Task<GasInspectionState> CompleteGasInspectionAsync(
        Guid id, decimal co, decimal co2, decimal o2, int hc, int no, decimal lambda, DateTimeOffset completedAt)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        var gasInspection = await dbContext.GasInspections.SingleAsync(x => x.Id == id);

        gasInspection.CO = co;
        gasInspection.CO2 = co2;
        gasInspection.O2 = o2;
        gasInspection.HC = hc;
        gasInspection.NO = no;
        gasInspection.Lambda = lambda;
        gasInspection.CompletedOn = completedAt;

        await dbContext.SaveChangesAsync();

        return gasInspection;
    }
}
