using System.Threading;
using System.Threading.Tasks;

namespace GEC.InspectionService.Infrastructure.IPCamera.Services.Abstractions;

public interface ICameraSnapshotService
{
    Task<byte[]> CapturePngAsync(CancellationToken ct = default);
}