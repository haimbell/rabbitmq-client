using System.Collections.Concurrent;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Abstractions.MessageHandlers;

namespace RabbitMq.Client.Implementations;

public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
    private readonly ConcurrentDictionary<string, List<SubscriptionInfo>> _handlers = new();
    private readonly ConcurrentDictionary<string, Type> _eventTypes = new();

    public void AddSubscription<T, TH>(string queueName, string eventName)
        where TH : IMessageHandler
    {
        var key = $"{queueName}_{eventName}";
        DoAddSubscription(typeof(TH), typeof(T)
            , queueName, eventName);

        if (!_eventTypes.ContainsKey(key))
        {
            _eventTypes[key] = typeof(T);
        }
    }

    private void DoAddSubscription(Type handlerType, Type messageType, string queueName, string eventName)
    {
        string key = $"{queueName}_{eventName}";
        if (!HasSubscriptionsForEvent( queueName, eventName))
        {
            while (!_handlers.TryAdd($"{queueName}_{eventName}", new List<SubscriptionInfo>()))
            { }
        }

        if (_handlers[key].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));
        }
        _handlers[key].Add(new(handlerType, messageType));

    }

    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string queueName, string eventName)
        => _handlers[$"{queueName}_{eventName}"].ToList();

    public bool HasSubscriptionsForEvent(string queueName, string eventName)
        => _handlers.ContainsKey($"{queueName}_{eventName}");
   
    public Type? GetEventTypeByName(string queueName, string eventName)
    {
        _eventTypes.TryGetValue($"{queueName}_{eventName}", out var eventType);
        return eventType;
    }
}