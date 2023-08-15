using System.Collections.Concurrent;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Abstractions.MessageHandlers;

namespace RabbitMq.Client.Implementations;

public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly ConcurrentDictionary<string, List<SubscriptionInfo>> _handlers = new();
    private readonly ConcurrentDictionary<string, Type> _eventTypes = new();
    ConcurrentDictionary<string, List<string>> _routingKeys = new();
    public void AddSubscription<T, TH>(string exchange, string queueName, string eventName)
        where TH : IMessageHandler
    {
        string key = $"{exchange}_{queueName}_{eventName}";
        if (!HasSubscriptionsForEvent(exchange, queueName, eventName))
        {
            while (!_handlers.TryAdd(key, new List<SubscriptionInfo>()))
            { }
        }

        var handlerType = typeof(TH);
        var messageType = typeof(T);
        if (_handlers[key].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }
        _handlers[key].Add(new(handlerType, messageType));
        //if (!_eventTypes.ContainsKey(key)) 
        _eventTypes[key] = messageType;


    }

    public void AddSubscription(string exchange, string queue, string eventName)
    {
        var key = $"{exchange}_{queue}".ToLower();
        if (_routingKeys.TryGetValue(key, out var keys))
        {
            keys.Add(eventName);
        }
        else
        {
            _routingKeys[key] = new List<string>() { eventName };
        }
    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string exchange, string queueName, string eventName)
        => _handlers[$"{exchange}_{queueName}_{eventName}"].ToList();

    public bool HasSubscriptionsForEvent(string exchange, string queueName, string eventName)
        => _handlers.ContainsKey($"{exchange}_{queueName}_{eventName}");

    public IEnumerable<string> GetRoutingKeys(string exchange, string queue)
    {
        var key = $"{exchange}_{queue}".ToLower();
        return _routingKeys.TryGetValue(key, out var keys) ? keys : new List<string>();
    }
    public Type? GetEventTypeByName(string exchange, string queueName, string eventName)
    {
        _eventTypes.TryGetValue($"{queueName}_{eventName}", out var eventType);
        return eventType;
    }
}