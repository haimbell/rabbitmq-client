using Crawler.WebApi.RabbitMq;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace Controllers.Samples;

public class AccountCreatedMessageHandler : IMessageHandler<AccountCreatedIntegrationEvent>
{
    private readonly ILogger<AccountCreatedMessageHandler> _logger;

    public AccountCreatedMessageHandler(ILogger<AccountCreatedMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AccountCreatedIntegrationEvent model, EventArgs args)
    {
        try
        {
            var basicDeliverEventArgs = args as BasicDeliverEventArgs;
            _logger.LogInformation($"done {basicDeliverEventArgs.RoutingKey}");
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create crawler account {event}", model);
            throw;
        }
    }
}