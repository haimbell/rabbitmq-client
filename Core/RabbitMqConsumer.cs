using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Abstractions.Controllers;
using RabbitMq.Client.Abstractions.MessageHandlers;
using RabbitMq.Client.Core.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;
using System.Reflection;
using System.Text;

namespace RabbitMq.Client.Core;

public class RabbitMqConsumer : IDisposable
{
    private readonly IRabitMqPersistenceConnection _rabitMqPersistenceConnection;
    private readonly IOptions<RabbitMqConsumerOptions> _consumerOptions;
    private readonly IOptions<RabbitMqServerOptions> _serverOptions;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private string QueueName => _consumerOptions.Value.QueueName;
    private readonly IEventBusSubscriptionsManager _subsManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly IJsonSerializer _jsonSerializer;
    public bool IsConnected => _rabitMqPersistenceConnection.IsConnected && _channel.IsOpen;

    public RabbitMqConsumer(IRabitMqPersistenceConnection rabitMqPersistenceConnection, IOptions<RabbitMqConsumerOptions> consumerOptions, ILogger<RabbitMqConsumer> logger,
        IEventBusSubscriptionsManager subsManager, IServiceProvider serviceProvider,
        IJsonSerializer jsonSerializer, IOptions<RabbitMqServerOptions> serverOptions)
    {
        _rabitMqPersistenceConnection = rabitMqPersistenceConnection;
        _logger = logger;
        _subsManager = subsManager;
        _serviceProvider = serviceProvider;
        _jsonSerializer = jsonSerializer;
        _consumerOptions = consumerOptions;
        _serverOptions = serverOptions;

        _channel = _rabitMqPersistenceConnection.CreateModel();
    }

    public void Configure(Action<RabbitMqServerOptions> configureServer,
        Action<RabbitMqConsumerOptions> configureConsumer)
    {
        configureServer(_serverOptions.Value);
        configureConsumer(_consumerOptions.Value);
    }

    public void Migrate()
    {
        _logger.LogInformation("Migrating queue {QueueName}", QueueName);
        _channel.ExchangeDeclare(exchange: _consumerOptions.Value.Exchange, type: ExchangeType.Direct);

        _logger.LogInformation("Exchange {ExchangeName} declared", _consumerOptions.Value.Exchange);

        _channel.QueueDeclare(queue: QueueName,
            durable: false, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>()
            {
                {"message-ttl",600}
            });
        _logger.LogInformation("Queue {QueueName} declared", QueueName);
    }

    public void AddSubscription(string eventName)
    {
        _logger.LogInformation("Adding subscription for ({Queue}) --> ({EventName})"
            , _consumerOptions.Value.QueueName, eventName);

        _channel.QueueBind(queue: QueueName, exchange: _consumerOptions.Value.Exchange,
            routingKey: eventName);
    }

    public void AddSubscription<T, TH>(string? eventName = null)
        where TH : IMessageHandler
    {
        if (eventName == null)
        {
            var type = typeof(T);
            var routingKeyAttribute = type.GetCustomAttribute<RoutingKeyAttribute>();
            eventName = routingKeyAttribute == null ? nameof(T) : routingKeyAttribute.Name;
        }

        _logger.LogInformation("Adding subscription for ({Queue}) --> ({EventName})"
            , _consumerOptions.Value.QueueName, eventName);

        _subsManager.AddSubscription<T, TH>(_consumerOptions.Value.Exchange, QueueName, eventName);
        _channel.QueueBind(queue: QueueName, exchange: _consumerOptions.Value.Exchange,
            routingKey: eventName);
    }

    private CancellationToken _cancellationToken;
    private readonly IModel _channel;

    public void StartConsuming(CancellationToken cancellationToken = default)
    {
        if (_consumerOptions.Value.QueueName is null)
            throw new ArgumentNullException(nameof(_consumerOptions.Value.QueueName));
        if (_consumerOptions.Value.Exchange is null)
            throw new ArgumentNullException(nameof(_consumerOptions.Value.Exchange));
        _cancellationToken = cancellationToken;
        _logger.LogInformation("Start consuming queue {QueueName}", QueueName);

        _logger.LogInformation("Binding queue {QueueName} to exchange {ExchangeName}", QueueName, QueueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnConsumerOnReceived;
        _channel.BasicQos(0, 1, false);
        _channel.BasicConsume(queue: QueueName,
            autoAck: false, consumer: consumer, consumerTag: "ss");

        _logger.LogInformation("Start consuming queue {QueueName} completed", QueueName);
    }

    private async Task OnConsumerOnReceived(object sender, BasicDeliverEventArgs e)
    {
        try
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _logger.LogTrace("Received RabbitMQ event: {EventName}", e.RoutingKey);
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            if (await ProcessEvent(e.RoutingKey, message, e))
                _channel.BasicAck(e.DeliveryTag, false);
            else
                _channel.BasicReject(e.DeliveryTag, false);
            _logger.LogTrace("Processed RabbitMQ event: {EventName}", e.RoutingKey);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("No service for type") && ex.Message.Contains("has been registered"))
        {
            _logger.LogCritical(ex, "Cannot handle message because service is not registered");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming RabbitMQ event: {EventName}", e.RoutingKey);
            throw;
        }
    }

    private async Task<bool> ProcessEvent(string eventName, string message, BasicDeliverEventArgs eventArgs)
    {
        _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        using var scope = _serviceProvider.CreateScope();
        foreach (var valueMiddelware in _consumerOptions.Value.Middelwares)
        {
            valueMiddelware(scope.ServiceProvider, eventArgs);
        }
        var routingKeyMethodsCache = scope.ServiceProvider.GetRequiredService<IRoutingKeyMethodsCache>();
        var excutionMethod = routingKeyMethodsCache.Get(eventName, _consumerOptions.Value.QueueName, _consumerOptions.Value.Exchange);
        if (excutionMethod != null)
        {
            await TriggerControllerMethod(excutionMethod, eventArgs, message, scope.ServiceProvider);
            return true;
        }
        else if (_subsManager.HasSubscriptionsForEvent(_consumerOptions.Value.Exchange, QueueName, eventName))
        {
            await TriggerHandler(eventName, message, eventArgs, scope);
            return true;
        }
        else
        {
            _logger.LogWarning("No subscription for RabbitMQ event: {EventName} {Body}", eventName, message);
            return false;
        }
    }

    private async Task TriggerHandler(string eventName, string message, BasicDeliverEventArgs eventArgs,
        IServiceScope scope)
    {
        var subscriptions = _subsManager.GetHandlersForEvent(_consumerOptions.Value.Exchange, QueueName, eventName);
        foreach (var subscription in subscriptions)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            //using (LogContext.PushProperty("TenantId", eventArgs.BasicProperties.TenantId))
            using (LogContext.PushProperty("CorrelationId", eventArgs.BasicProperties.CorrelationId))
            using (LogContext.PushProperty("MessageId", eventArgs.BasicProperties.MessageId))
            using (LogContext.PushProperty("EventType", eventName))
            {
                var handler = scope.ServiceProvider.GetService(subscription.HandlerType);
                if (handler == null) continue;
                var eventType = _subsManager.GetEventTypeByName(_consumerOptions.Value.Exchange, QueueName, eventName);

                if (typeof(IMessageHandler).IsAssignableFrom(subscription.HandlerType))
                {
                    var rabbitMessage = _jsonSerializer.Deserialize(message, subscription.MessageType);

                    _logger.LogTrace("Handle {@MqMessage}", rabbitMessage);
                    var method = subscription.HandlerType.GetMethod("Handle");
                    await (Task)method.Invoke(handler, new object[] { rabbitMessage, (EventArgs)eventArgs });
                }
            }
        }
    }

    private async Task TriggerControllerMethod(ExcutionMethod excutionMethod, BasicDeliverEventArgs eventArgs,
        string message, IServiceProvider serviceProvider)
    {
        //using (LogContext.PushProperty("TenantId", currentContext.TenantId))
        using (LogContext.PushProperty("CorrelationId", eventArgs.BasicProperties.CorrelationId))
        using (LogContext.PushProperty("MessageId", eventArgs.BasicProperties.MessageId))
        using (LogContext.PushProperty("EventType", eventArgs.RoutingKey))
        {
            var methodsParams = PopulateMethodsParams(excutionMethod, message, eventArgs, serviceProvider);

            var controller = (BrokerControllerBase)serviceProvider.GetRequiredService(excutionMethod.MethodInfo.ReflectedType);
            controller.SetServiceProvider(serviceProvider);
            //if method is async execute with await, otherwise execute sync
            if (excutionMethod.MethodInfo.ReturnType != typeof(Task))
                excutionMethod.MethodInfo.Invoke(controller, methodsParams);
            else
                await (Task)excutionMethod.MethodInfo.Invoke(controller, methodsParams);
        }
    }

    private object[] PopulateMethodsParams(ExcutionMethod excutionMethod, string message, BasicDeliverEventArgs eventArgs,
        IServiceProvider serviceProvider)
    {
        object[] methodsParams = new object[excutionMethod.Params.Length];
        for (var i = 0; i < excutionMethod.Params.Length; i++)
        {
            ExcutionMethodParam? excutionMethodParam = excutionMethod.Params[i];
            if (excutionMethodParam.Source == ExcutionParamSoruce.Message)
                methodsParams[i] = _jsonSerializer.Deserialize(message, excutionMethodParam.Type);
            else if (excutionMethodParam.Source == ExcutionParamSoruce.Service)
                methodsParams[i] = serviceProvider.GetRequiredService(excutionMethodParam.Type);
            else if (typeof(EventArgs).IsAssignableFrom(excutionMethodParam.Type))
                methodsParams[i] = eventArgs;
            else if (typeof(CancellationToken).IsAssignableFrom(excutionMethodParam.Type))
                methodsParams[i] = _cancellationToken;
            else
                throw new ArgumentException($"Invalid parameter source {excutionMethodParam.Source}");
        }

        return methodsParams;
    }

    public void StopConsuming()
    {
        _channel.Close();
    }

    public void Dispose()
    {
        _rabitMqPersistenceConnection.Dispose();
        _channel.Dispose();
    }
}