namespace RabbitMq.Client.Core;

public record RabbitMqConsumerOptions
{
    public string? QueueName { get; set; }
    public string? Exchange { get; set; }
    public List<Action<IServiceProvider, EventArgs>> Middelwares { get; set; } = new();
}
public record RabbitMqServerOptions
{
    public string[] HostNames { get; set; }
    public int? Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string ClientProvidedName { get; set; }
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public Dictionary<string, object>? Arguments { get; set; } = null;
}