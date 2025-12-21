using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Contracts.Commands;

using GEC.InspectionService.Infrastructure.IPCamera.Services.Abstractions;
using GEC.InspectionService.Infrastructure.PlateRecognizer.Services.Abstractions;
using GEC.InspectionService.Services.Abstractions;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GEC.InspectionService.Messaging.CommandHandlers;

public class RegisterNewGasDataCommandHandler : AsyncEventingBasicConsumer
{
    private readonly JsonSerializerOptions _jsonOptions = new();
    private readonly IGasInspectionService _gasInspectionService;
    private readonly ICameraSnapshotService _cameraSnapshotService;
    private readonly IPlateRecognizerAdapterService _plateRecognizerAdapterService;
    private readonly ILogger<RegisterNewGasDataCommandHandler> _logger;
    private readonly CancellationToken _cancellationToken;

    public RegisterNewGasDataCommandHandler(
        IChannel channel,
        IGasInspectionService gasInspectionService,
        ICameraSnapshotService cameraSnapshotService,
        IPlateRecognizerAdapterService plateRecognizerAdapterService,
        ILogger<RegisterNewGasDataCommandHandler> logger,
        CancellationToken cancellationToken) : base(channel)
    {
        _gasInspectionService = gasInspectionService;
        _cameraSnapshotService = cameraSnapshotService;
        _plateRecognizerAdapterService = plateRecognizerAdapterService;
        _logger = logger;
        _cancellationToken = cancellationToken;

        ReceivedAsync += MessageReceivedAsync;
    }

    private async Task MessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var command = (RegisterNewGasDataCommand)null;
        try
        {
            var commandBody = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogInformation("Command of type {CommandType} with body {CommandBody} was received", typeof(RegisterNewGasDataCommand), commandBody);

            command = JsonSerializer.Deserialize<RegisterNewGasDataCommand>(commandBody, _jsonOptions);

            var currentCameraSnapshot = await _cameraSnapshotService.CapturePngAsync(_cancellationToken);

            if (currentCameraSnapshot.Length == 0)
            {
                throw new Exception("Error taking snapshot");
            }
            _logger.LogDebug("Snapshot taken successfully");

            var registerNumber = await _plateRecognizerAdapterService.TryGetPlateNumberAsync(currentCameraSnapshot);
            if (string.IsNullOrEmpty(registerNumber))
            {
                throw new Exception("Error plate number recognition");
            }

            var gasInspection = await _gasInspectionService.StartGasInspectionAsync(command.CorrelationId, registerNumber, command.StartedAt, _cancellationToken);

            _logger.LogInformation("Successfully started gas inspection with id={GasInspectionId}", gasInspection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when consuming {CommandType} command with CorrelationId={CorrelationId}", typeof(RegisterNewGasDataCommand), command?.CorrelationId);
        }

        await Channel.BasicAckAsync(ea.DeliveryTag, false);
    }
}
