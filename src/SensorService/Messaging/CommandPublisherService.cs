using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using GasEmissionsCheck.Common.Contracts.Commands;
using GasEmissionsCheck.SensorService.Messaging.Abstractions;
using GasEmissionsCheck.SensorService.Module;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace GasEmissionsCheck.SensorService.Messaging;

public class CommandPublisherService : ICommandPublisher, IAsyncDisposable
{
    private readonly RabbitMQSettings _rabbitMQSettings;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection _connection;
    private readonly ConcurrentBag<IChannel> _publishPool = [];
    private readonly SemaphoreSlim _publishGate;
    private readonly int _poolSize;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public CommandPublisherService(IOptions<RabbitMQSettings> rabbitMQSettings)
    {
        _rabbitMQSettings = rabbitMQSettings.Value;

        _connectionFactory = new ConnectionFactory
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

        _poolSize = _rabbitMQSettings.PublisherPoolSize;
        _publishGate = new SemaphoreSlim(_poolSize, _poolSize);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return;
            }

            while (_publishPool.TryTake(out var channelToDispose))
            {
                try { channelToDispose.Dispose(); } catch { }
            }

            if (_connection != null)
            {
                try
                {
                    if (_connection.IsOpen)
                    {
                        await _connection.CloseAsync(cancellationToken);
                    }
                }
                catch { }
                try
                {
                    _connection.Dispose();
                }
                catch { }

                _connection = null;
            }

            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            var channel = await GetChannelAsync(cancellationToken);
            try
            {
                await channel.QueueDeclareAsync(
                    queue: _rabbitMQSettings.RegisterNewGasDataQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                await channel.QueueDeclareAsync(
                    queue: _rabbitMQSettings.CompleteGasDataMeasuringQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                ReturnChannel(channel);
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync(RegisterNewGasDataCommand command, CancellationToken cancellationToken)
    {
        await PublishAsync(_rabbitMQSettings.RegisterNewGasDataQueue, command, cancellationToken);
    }

    public async Task PublishAsync(CompleteGasDataMeasuringCommand command, CancellationToken cancellationToken)
    {
        await PublishAsync(_rabbitMQSettings.CompleteGasDataMeasuringQueue, command, cancellationToken);
    }

    private async Task PublishAsync<TMessage>(string queueName, TMessage message, CancellationToken cancellationToken)
    {
        await _publishGate.WaitAsync(cancellationToken);

        IChannel channel = null;

        try
        {
            channel = await GetChannelAsync(cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var basicProperties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Type = typeof(TMessage).Name
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: basicProperties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (AlreadyClosedException)
        {
            channel = null;

            throw;
        }
        catch (BrokerUnreachableException)
        {
            channel = null;

            throw;
        }
        finally
        {
            if (channel != null)
            {
                ReturnChannel(channel);
            }

            _publishGate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_publishPool.TryTake(out var channel))
        {
            if (channel.IsOpen)
            {
                return channel;
            }

            channel.Dispose();
        }

        channel = await _connection.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken);

        return channel;
    }

    private void ReturnChannel(IChannel channel) => _publishPool.Add(channel);

    public async ValueTask DisposeAsync()
    {
        while (_publishPool.TryTake(out var channel))
        {
            try
            {
                channel.Dispose();
            }
            catch { }
        }

        _publishGate.Dispose();
        _initLock.Dispose();

        if (_connection != null)
        {
            try
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync();
                }
            }
            catch { }

            try
            {
                _connection.Dispose();
            }
            catch { }

            _connection = null;
        }

        GC.SuppressFinalize(this);
    }
}
