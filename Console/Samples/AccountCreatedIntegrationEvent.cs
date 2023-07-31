using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Implementations;

namespace Console.Samples;

[RoutingKey(nameof(AccountCreatedIntegrationEvent))]
public class AccountCreatedIntegrationEvent : IntegrationEvent { }