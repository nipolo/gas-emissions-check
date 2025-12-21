using System;
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
    private readonly AppSettings _appSettings;
    private readonly DomainSettings _domainSettings;
    private readonly IGasAnalyzerDataService _gasAnalyzerDataService;
    private readonly ICommandPublisher _commandPublisher;
    private readonly ILogger<GasAnalyzerSensorWorker> _logger;

    private SerialPort _serialPort;

    // session state
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

        _logger.LogInformation(
            "Worker started. GasAnalyzerCOMPort {GasAnalyzerCOMPort}",
            _appSettings.GasAnalyzerCOMPort);

        await base.StartAsync(cancellationToken);
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
            if (_serialPort is null || !_serialPort.IsOpen)
            {
                return;
            }

            var bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead <= 0)
            {
                return;
            }

            var buffer = new byte[bytesToRead];
            var read = _serialPort.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                return;
            }

            _logger.LogDebug("{Time} {SerializedBuffer}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), string.Join("-", buffer));

            ProcessSerialReadBuffer(buffer);
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
            return;
        }

        _logger.LogDebug("Buffer is in gas data format");

        if (!_isInSession)
        {
            if (data.CO >= _domainSettings.COStartThreshold)
            {
                StartSession(data);
            }

            return;
        }

        if (data.CO >= _domainSettings.COStartThreshold)
        {
            CheckForBetterLambda(data);
        }
        else
        {
            CompleteSession();
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
