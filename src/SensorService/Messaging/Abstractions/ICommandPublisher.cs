using System.Threading;
using System.Threading.Tasks;

using GasEmissionsCheck.Common.Contracts.Commands;

namespace GasEmissionsCheck.SensorService.Messaging.Abstractions;

public interface ICommandPublisher
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task PublishAsync(RegisterNewGasDataCommand command, CancellationToken cancellationToken);

    Task PublishAsync(CompleteGasDataMeasuringCommand command, CancellationToken cancellationToken);
}
