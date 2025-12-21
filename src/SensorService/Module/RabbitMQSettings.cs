namespace GEC.SensorService.Module;

public class RabbitMQSettings
{
    public const string Key = nameof(RabbitMQSettings);

    public string Host { get; set; }

    public string VirtualHost { get; set; }

    public int Port { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string RegisterNewGasDataQueue { get; set; }

    public string CompleteGasDataMeasuringQueue { get; set; }

    public string ClientName { get; set; }

    public int PublisherPoolSize { get; set; }
}
