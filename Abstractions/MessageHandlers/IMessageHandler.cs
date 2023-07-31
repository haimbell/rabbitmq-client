namespace RabbitMq.Client.Abstractions.MessageHandlers;

public interface IMessageHandler { };
public interface IMessageHandler<in T> : IMessageHandler
    where T : new()
{
    Task Handle(T model, EventArgs args);
}
