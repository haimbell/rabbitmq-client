using Crawler.WebApi.RabbitMq;

namespace Controllers.Samples;

[RoutingKey(nameof(AccountCreatedIntegrationEvent))]
public class AccountCreatedIntegrationEvent : IntegrationEvent { }