using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Contracts.Commands;

namespace GEC.SensorService.Messaging.Abstractions;

public interface ICommandPublisher
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task PublishAsync(RegisterNewGasDataCommand command, CancellationToken cancellationToken);

    Task PublishAsync(CompleteGasDataMeasuringCommand command, CancellationToken cancellationToken);
}
