namespace RabbitMq.Client.Abstractions.IntegrationEvents;

/// <summary>
/// Backwards compatibility
/// </summary>
[Obsolete]
public interface IIntegrationEvent
{
    public Guid Id { get; }
}