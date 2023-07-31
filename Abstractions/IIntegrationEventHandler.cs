namespace Crawler.WebApi.RabbitMq;

public interface IIntegrationEventHandler: IMessageHandler { };
public interface IIntegrationEventHandler<in T> : IIntegrationEventHandler
    where T : IIntegrationEvent
{
    Task Handle(T model);
}

public interface IIntegrationEvent
{
    public Guid Id { get; }
}