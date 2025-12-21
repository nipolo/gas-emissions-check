using System.Threading;
using System.Threading.Tasks;

namespace GEC.InspectionService.Infrastructure.PlateRecognizer.Services.Abstractions;

public interface IPlateRecognizerAdapterService
{
    Task<string> TryGetPlateNumberAsync(byte[] pngImageBytes, CancellationToken cancellationToken = default);
}