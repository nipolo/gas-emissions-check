using System.Collections.Generic;

namespace GEC.InspectionService.Module;

public class RabbitMQSettings
{
    public const string Key = nameof(RabbitMQSettings);

    public string Host { get; set; }

    public string VirtualHost { get; set; }

    public int Port { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string ClientName { get; set; }

    public Dictionary<string, string> Queues { get; set; }
}
