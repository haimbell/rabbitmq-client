using RabbitMq.Client.Abstractions.MessageHandlers;

namespace RabbitMq.Client.Abstractions;

public interface IEventBusSubscriptionsManager
{
    void AddSubscription<T, TH>(string exchange, string queue, string eventName)
        where TH : IMessageHandler;

    void AddSubscription(string exchange, string queue, string eventName);
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string exchange, string queue, string eventName);
    IEnumerable<string> GetRoutingKeys(string exchange, string queue);
    bool HasSubscriptionsForEvent(string exchange, string queue, string eventName);
    Type? GetEventTypeByName(string exchange, string queueName, string eventName);
}

public record SubscriptionInfo(Type HandlerType, Type MessageType);