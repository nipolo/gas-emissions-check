using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Contracts.Commands;

using GEC.InspectionService.Services.Abstractions;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GEC.InspectionService.Messaging.CommandHandlers;

public class CompleteGasDataMeasuringCommandHandler : AsyncEventingBasicConsumer
{
    private readonly JsonSerializerOptions _jsonOptions = new();
    private readonly IGasInspectionService _gasInspectionService;
    private readonly ILogger<CompleteGasDataMeasuringCommandHandler> _logger;
    private readonly CancellationToken _cancellationToken;

    public CompleteGasDataMeasuringCommandHandler(
        IChannel channel,
        IGasInspectionService gasInspectionService,
        ILogger<CompleteGasDataMeasuringCommandHandler> logger,
        CancellationToken cancellationToken) : base(channel)
    {
        _gasInspectionService = gasInspectionService;
        _logger = logger;
        _cancellationToken = cancellationToken;
        ReceivedAsync += MessageReceivedAsync;
    }

    private async Task MessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var command = (CompleteGasDataMeasuringCommand)null;
        try
        {
            var commandBody = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogInformation("Command of type {CommandType} with body {CommandBody} was received", typeof(CompleteGasDataMeasuringCommand), commandBody);

            command = JsonSerializer.Deserialize<CompleteGasDataMeasuringCommand>(commandBody, _jsonOptions);

            var gasInspection = await _gasInspectionService.CompleteGasInspectionAsync(command.CorrelationId, command.CO, command.CO2, command.O2, command.HC, command.NO, command.Lambda, command.CompletedAt, _cancellationToken);

            _logger.LogInformation("Successfully completed gas inspection with id={GasInspectionId}", gasInspection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when consuming {CommandType} command with CorrelationId={CorrelationId}", typeof(CompleteGasDataMeasuringCommand), command?.CorrelationId);
        }

        await Channel.BasicAckAsync(ea.DeliveryTag, false);
    }
}
