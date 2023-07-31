using RabbitMq.Client.Abstractions.MessageHandlers;

namespace RabbitMq.Client.Abstractions;

public interface IEventBusSubscriptionsManager
{
    void AddSubscription<T, TH>(string queue, string eventName)
        where TH : IMessageHandler;
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string queue, string eventName);
    bool HasSubscriptionsForEvent(string queue, string eventName);
    Type? GetEventTypeByName(string queueName, string eventName);
}

public record SubscriptionInfo(Type HandlerType, Type MessageType);