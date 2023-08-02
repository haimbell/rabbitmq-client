using RabbitMQ.Client;

namespace RabbitMq.Client.Core.Connection;

public interface IRabitMqPersistenceConnection : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    IModel CreateModel();
}