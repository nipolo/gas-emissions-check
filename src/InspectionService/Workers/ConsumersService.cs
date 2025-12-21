using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using GEC.InspectionService.Module;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

using RabbitMQ.Client.Events;

namespace GEC.InspectionService.Workers;

public sealed class ConsumersService : BackgroundService
{
    private IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private readonly ILogger<ConsumersService> _logger;

    private readonly List<IChannel> _channels = [];
    private readonly List<string> _consumerTags = [];
    private readonly List<IServiceScope> _scopes = [];

    public ConsumersService(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQSettings> rabbitMQSettings,
        ILogger<ConsumersService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMQSettings = rabbitMQSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartAllConsumersAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public async Task StartAllConsumersAsync(CancellationToken cancellationToken)
    {
        _connection = await CreateConnectionAsync(cancellationToken);

        var assembly = Assembly.GetExecutingAssembly();

        var handlerTypes = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(AsyncEventingBasicConsumer).IsAssignableFrom(t))
            .ToList();

        if (handlerTypes.Count == 0)
        {
            _logger.LogWarning("No consumer handlers found (types inheriting AsyncEventingBasicConsumer).");

            return;
        }

        _logger.LogInformation("Discovered {Count} consumer handler(s): {Handlers}",
            handlerTypes.Count,
            string.Join(", ", handlerTypes.Select(t => t.Name)));

        foreach (var handlerType in handlerTypes)
        {
            var queueName = _rabbitMQSettings.Queues.GetValueOrDefault(handlerType.Name);

            if (queueName is null)
            {
                _logger.LogWarning(
                    "Skipping handler {HandlerType} because no queue mapping was found in RabbitMQSettings.Queues",
                    handlerType.Name);

                continue;
            }

            var scope = _serviceProvider.CreateScope();
            _scopes.Add(scope);

            var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            _channels.Add(channel);

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 10,
                global: false,
                cancellationToken: cancellationToken);

            var consumer = (AsyncEventingBasicConsumer)ActivatorUtilities.CreateInstance(
                scope.ServiceProvider,
                handlerType,
                channel);

            var tag = await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _consumerTags.Add(tag);

            _logger.LogInformation(
                "Started consumer. Handler={HandlerType} Queue={Queue} ConsumerTag={Tag}",
                handlerType.Name, queueName, tag);
        }

        if (_consumerTags.Count == 0)
        {
            _logger.LogWarning("No consumers were started (no handler matched RabbitMQSettings.Queues mapping).");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < _channels.Count && i < _consumerTags.Count; i++)
        {
            var channel = _channels[i];
            var tag = _consumerTags[i];

            if (channel.IsOpen)
            {
                try
                {
                    await channel.BasicCancelAsync(tag, cancellationToken: CancellationToken.None);
                }
                catch { }
            }
        }

        foreach (var channel in _channels)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync(CancellationToken.None);
                }

                await channel.DisposeAsync();
            }
            catch { }
        }

        _channels.Clear();
        _consumerTags.Clear();

        if (_connection is not null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync(CancellationToken.None);
                }

                _connection.Dispose();
            }
            catch { }
            finally
            {
                _connection = null;
            }
        }

        foreach (var scope in _scopes)
        {
            try
            {
                scope.Dispose();
            }
            catch { }
        }
        _scopes.Clear();

        await base.StopAsync(cancellationToken);
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _rabbitMQSettings.Host,
            VirtualHost = _rabbitMQSettings.VirtualHost,
            Port = _rabbitMQSettings.Port,
            UserName = _rabbitMQSettings.Username,
            Password = _rabbitMQSettings.Password,
            ClientProvidedName = _rabbitMQSettings.ClientName,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        return await connectionFactory.CreateConnectionAsync(cancellationToken);
    }
}
