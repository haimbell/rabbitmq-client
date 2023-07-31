using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace RabbitMq.Client.Core;

public class RabbitMqConfigurator : IDisposable
{
    private readonly ILogger<RabbitMqConfigurator> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqConfigurator(IOptions<RabbitMqServerOptions> options, ILogger<RabbitMqConfigurator> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory
        {

            Port = options.Value.Port.GetValueOrDefault(AmqpTcpEndpoint.UseDefaultPort),
            UserName = options.Value.UserName,
            Password = options.Value.Password,
            VirtualHost = options.Value.VirtualHost,
            ClientProvidedName = options.Value.ClientProvidedName,
            AutomaticRecoveryEnabled = options.Value.AutomaticRecoveryEnabled,
            RequestedConnectionTimeout = new TimeSpan(30000),
            RequestedHeartbeat = new TimeSpan(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true,
            DispatchConsumersAsync = true,
            ContinuationTimeout = TimeSpan.FromSeconds(10),
            EndpointResolverFactory = endpoints =>
            {
                string[] hostNames = options.Value.HostNames;
                return hostNames is { Length: > 0 } ?
                    new DefaultEndpointResolver(options.Value.HostNames.Select(ep =>
                        new AmqpTcpEndpoint(ep, options.Value.Port.GetValueOrDefault(-1))))
                    : (IEndpointResolver)new DefaultEndpointResolver(endpoints);
            }
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

    }


    public void Migrate(RabbitMqMigrationOptions options)
    {


        _logger.LogInformation("Migration started");
        _channel.ExchangeDeclare(exchange: options.Exchange, type: options.ExchangeType);

        _logger.LogInformation("Exchange {ExchangeName} declared", options.Exchange);

        foreach (var migrationQueueOptions in options.Queues)
        {
            _channel.QueueDeclare(queue: migrationQueueOptions.Name,
                durable: false, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>()
                {
                    {"message-ttl",600}
                });
            _logger.LogInformation("Queue {QueueName} declared", options.ExchangeType);

            foreach (var routingKey in migrationQueueOptions.RoutingKeys)
            {
                _channel.QueueBind(queue: migrationQueueOptions.Name, options.Exchange, routingKey);
            }

        }
    }



    public void Dispose()
    {
        _connection.Dispose();
        _channel.Dispose();
    }
}

public class RabbitMqMigrationQueueOptions
{
    public string Name { get; set; } = null!;
    public Dictionary<string, object> Args { get; set; } = null!;

    public List<string> RoutingKeys { get; set; } = null!;
    public bool Durable { get; set; }
    public bool AutoDelete { get; set; }
}
public class RabbitMqMigrationOptions
{
    public string Exchange { get; set; } = null!;
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Direct;
    public List<RabbitMqMigrationQueueOptions> Queues { get; init; } = new();
}