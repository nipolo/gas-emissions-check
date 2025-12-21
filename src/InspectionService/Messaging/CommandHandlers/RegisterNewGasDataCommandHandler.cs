using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using GasEmissionsCheck.Common.Contracts.Commands;

using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GEC.InspectionService.Messaging.CommandHandlers;

public class RegisterNewGasDataCommandHandler : AsyncEventingBasicConsumer
{
    private readonly JsonSerializerOptions _jsonOptions = new();
    private readonly ILogger<RegisterNewGasDataCommandHandler> _logger;

    public RegisterNewGasDataCommandHandler(
        IChannel channel,
        ILogger<RegisterNewGasDataCommandHandler> logger) : base(channel)
    {
        _logger = logger;

        ReceivedAsync += MessageReceivedAsync;
    }

    private async Task MessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            var command = JsonSerializer.Deserialize<RegisterNewGasDataCommand>(json, _jsonOptions);

            // business logic here

            await Channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch
        {
            await Channel.BasicNackAsync(ea.DeliveryTag, false, true);
        }
    }
}
