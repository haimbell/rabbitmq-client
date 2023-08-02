using Microsoft.Extensions.Logging;
using Polly;
using RabbitMq.Client.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using RabbitMq.Client.Core.Connection;

namespace RabbitMq.Client.Core;

public class RabbitMqProducer
{
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly IModel _channel;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IRabitMqPersistenceConnection _rabitMqPersistenceConnection;
    public RabbitMqProducer(ILogger<RabbitMqProducer> logger, IJsonSerializer jsonSerializer, IRabitMqPersistenceConnection rabitMqPersistenceConnection)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _rabitMqPersistenceConnection = rabitMqPersistenceConnection;
        _channel = _rabitMqPersistenceConnection.CreateModel();
    }

    public void Publish(object model, string? routingKey = null, Dictionary<string, object>? headers = null)
    {
        _rabitMqPersistenceConnection.TryConnect();
        if (routingKey == null)
        {
            var type = model.GetType();
            var routingKeyAttribute = type.GetCustomAttribute<RoutingKeyAttribute>();
            if (routingKeyAttribute?.Name != null)
                routingKey = routingKeyAttribute.Name;
            else
                routingKey = type.Name;
        }
        var properties = _channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var policy = Policy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})",
                    properties.MessageId, $"{time.TotalSeconds:n1}", ex.Message);
            });
        var message = _jsonSerializer.Serialize(model);
        var body = Encoding.UTF8.GetBytes(message);

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
                exchange: "exchange1",
                //exchange: _options.Value.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
            _logger.LogInformation("Published event: {MessageId} ({RoutingKey})", properties.MessageId, routingKey);
        });
    }

}