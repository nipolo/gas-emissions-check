using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

using GEC.Common.Contracts.Commands;

using GEC.Common.Shared.Utils;
using GEC.SensorService.Domain;
using GEC.SensorService.Messaging.Abstractions;
using GEC.SensorService.Module;
using GEC.SensorService.Services.Abstractions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GEC.SensorService.Workers;

public sealed class GasAnalyzerSensorWorker : BackgroundService
{
    private static readonly byte[] PingCommand = { 3, 2, 49, 82, 71, 67, 65 };
    private const int FrameLength = 43;
    private const byte FrameStartByte = 6;

    private readonly AppSettings _appSettings;
    private readonly DomainSettings _domainSettings;
    private readonly IGasAnalyzerDataService _gasAnalyzerDataService;
    private readonly ICommandPublisher _commandPublisher;
    private readonly ILogger<GasAnalyzerSensorWorker> _logger;

    private SerialPort _serialPort;

    // session state
    private readonly List<byte> _rxBuffer = new(FrameLength * 2);
    private bool _isInSession;
    private Guid _correlationId;
    private DateTimeOffset _startedAt;
    private GasAnalyzerData _bestData;

    public GasAnalyzerSensorWorker(
        IGasAnalyzerDataService gasAnalyzerDataService,
        ICommandPublisher commandPublisher,
        IOptions<AppSettings> appSettings,
        IOptions<DomainSettings> domainSettings,
        ILogger<GasAnalyzerSensorWorker> logger)
    {
        _gasAnalyzerDataService = gasAnalyzerDataService;
        _commandPublisher = commandPublisher;

        _appSettings = appSettings.Value;
        _domainSettings = domainSettings.Value;

        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _commandPublisher.InitializeAsync(cancellationToken);

            var portName = _appSettings.GasAnalyzerCOMPort ?? ComPortFinder.FindFirstComPortName();
            _serialPort = new SerialPort(portName)
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            _serialPort.DataReceived += OnSerialPortDataReceived;
            _serialPort.Open();
            _serialPort.Write(PingCommand, 0, PingCommand.Length);

            _logger.LogInformation(
                "Worker started. GasAnalyzerCOMPort {GasAnalyzerCOMPort}",
                _appSettings.GasAnalyzerCOMPort);

            await base.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error when starting {WorkerName}", nameof(GasAnalyzerSensorWorker));
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Delay(Timeout.Infinite, stoppingToken);

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_serialPort is not null)
            {
                _serialPort.DataReceived -= OnSerialPortDataReceived;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }

                _serialPort.Dispose();
                _serialPort = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping SerialPort.");
        }

        return base.StopAsync(cancellationToken);
    }

    private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs serialDataReceivedEventArgs)
    {
        try
        {
            var bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead <= 0)
            {
                return;
            }

            var chunk = new byte[bytesToRead];
            var read = _serialPort.Read(chunk, 0, chunk.Length);
            if (read <= 0)
            {
                return;
            }

            _rxBuffer.AddRange(chunk.AsSpan(0, read).ToArray());

            while (true)
            {
                var startIndex = _rxBuffer.IndexOf(FrameStartByte);
                if (startIndex < 0)
                {
                    _rxBuffer.Clear();

                    break;
                }

                if (startIndex > 0)
                {
                    _rxBuffer.RemoveRange(0, startIndex);
                }

                if (_rxBuffer.Count < FrameLength)
                {
                    break;
                }

                var frame = _rxBuffer.GetRange(0, FrameLength).ToArray();
                _rxBuffer.RemoveRange(0, FrameLength);

                _logger.LogDebug("{Time} {SerializedBuffer}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), string.Join("-", frame));

                ProcessSerialReadBuffer(frame);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Serial DataReceived error.");
        }
    }

    private void ProcessSerialReadBuffer(byte[] buffer)
    {
        if (!_gasAnalyzerDataService.TryParseData(buffer, out var data) || data is null)
        {
            _logger.LogDebug("Wrong format - skip buffer data");

            return;
        }

        _logger.LogDebug("Buffer is in gas data format");

        if (!_isInSession)
        {
            if (data.CO >= _domainSettings.COStartThreshold)
            {
                StartSession(data);
            }
        }
        else
        {
            if (data.CO >= _domainSettings.COStartThreshold)
            {
                CheckForBetterLambda(data);
            }
            else
            {
                CompleteSession();
            }
        }

        if (_serialPort?.IsOpen == true)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);

                if (_serialPort?.IsOpen == true)
                {
                    _serialPort.Write(PingCommand, 0, PingCommand.Length);
                }
            });
        }
    }

    private void StartSession(GasAnalyzerData first)
    {
        _isInSession = true;
        _correlationId = Guid.NewGuid();
        _startedAt = DateTimeOffset.UtcNow;
        _bestData = first;

        var command = new RegisterNewGasDataCommand
        {
            CorrelationId = _correlationId,
            StartedAt = _startedAt
        };

        _commandPublisher.PublishAsync(command, CancellationToken.None).GetAwaiter().GetResult();

        _logger.LogInformation("Measuring session started with CorrelationId={CorrelationId}", _correlationId);
    }

    private void CheckForBetterLambda(GasAnalyzerData current)
    {
        if (Math.Abs(1 - current.Lambda) < Math.Abs(1 - _bestData.Lambda))
        {
            _bestData = current;

            _logger.LogInformation("Session CorrelationId={CorrelationId} has new OptimalLambda={Lambda} and |1-Lambda|={Distance}", _correlationId, _bestData.Lambda, Math.Abs(1 - _bestData.Lambda));
        }
    }

    private void CompleteSession()
    {
        if (_bestData is null)
        {
            ResetSession();

            return;
        }

        var command = new CompleteGasDataMeasuringCommand()
        {
            CorrelationId = _correlationId,
            CompletedAt = DateTimeOffset.UtcNow,
            CO = _bestData.CO,
            CO2 = _bestData.CO2,
            HC = _bestData.HC,
            O2 = _bestData.O2,
            NO = _bestData.NO,
            Lambda = _bestData.Lambda
        };

        _commandPublisher.PublishAsync(command, CancellationToken.None).GetAwaiter().GetResult();

        _logger.LogInformation("Session with CorrelationId={CorrelationId} completed with OptimalLambda={Lambda}", _correlationId, _bestData.Lambda);

        ResetSession();
    }

    private void ResetSession()
    {
        _isInSession = default;
        _correlationId = default;
        _startedAt = default;
        _bestData = null;
    }
}
