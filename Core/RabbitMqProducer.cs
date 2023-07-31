using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Crawler.WebApi.RabbitMq;

public class RabbitMqProducer
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly IJsonSerializer _jsonSerializer;

    public RabbitMqProducer(IOptions<RabbitMqServerOptions> options, ILogger<RabbitMqProducer> logger, IJsonSerializer jsonSerializer)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
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
            EndpointResolverFactory = endpoints =>
            {
                string[] hostNames = options.Value.HostNames;
                return hostNames is { Length: > 0 } ?
                    new DefaultEndpointResolver(options.Value.HostNames.Select(ep =>
                        new AmqpTcpEndpoint(ep, options.Value.Port.GetValueOrDefault(-1))))
                    : (IEndpointResolver)new DefaultEndpointResolver(endpoints);
            }
        };
        //use polly to retry connection 5 time with 5 sec delay
        var retryPolicy = Policy.Handle<BrokerUnreachableException>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(5), (ex, time) =>
            {
                _logger.LogError(ex, "Could not connect to RabbitMQ after {TimeOut}s ({ExceptionMessage})",
                    $"{time.TotalSeconds:n1}", ex.Message);
            });

        retryPolicy.Execute(() =>
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        });
    }

    public void Publish(object model, string? routingKey = null, Dictionary<string, object>? headers = null)
    {

        if (routingKey == null)
        {
            var type = model.GetType();
            var routingKeyAttribute = type.GetCustomAttribute<RoutingKeyAttribute>();
            if (routingKeyAttribute?.Name != null)
                routingKey = routingKeyAttribute.Name;
            else
                routingKey = type.Name;
        }

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                //_logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
            });
        var message = _jsonSerializer.Serialize(model);
        var body = Encoding.UTF8.GetBytes(message);
        var properties = _channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        //properties.CorrelationId = _currentContext.CorrelationId;
        //properties.Headers = new Dictionary<string, object>
        //{
        //    {"TenantId", _currentContext.TenantId}
        //};
        if (headers != null)
            foreach (var eventHeader in headers)
            {
                properties.Headers[eventHeader.Key] = eventHeader.Value;
            }
        policy.Execute(() =>
        {

            _channel.BasicPublish(
                exchange: "crawler_api_2",
                //exchange: _options.Value.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
            _logger.LogInformation("Published event: {MessageId} ({RoutingKey})", properties.MessageId, routingKey);
        });
    }

}