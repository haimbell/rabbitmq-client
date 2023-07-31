using RabbitMq.Client.Abstractions.MessageHandlers;

namespace RabbitMq.Client.Abstractions.IntegrationEvents;
[Obsolete]
/// <summary>
/// Backwards compatibility
/// </summary>
public interface IIntegrationEventHandler : IMessageHandler { };
[Obsolete]
public interface IIntegrationEventHandler<in T> : IIntegrationEventHandler
    where T : IIntegrationEvent
{
    Task Handle(T model);
}