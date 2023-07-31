using Microsoft.Extensions.Logging;
using RabbitMq.Client.Abstractions.IntegrationEvents;

namespace Console.Samples;

public class AccountCreatedIntegrationEventHandler : IIntegrationEventHandler<AccountCreatedIntegrationEvent>
{
    private readonly ILogger<AccountCreatedIntegrationEventHandler> _logger;

    public AccountCreatedIntegrationEventHandler(ILogger<AccountCreatedIntegrationEventHandler> logger)
    {
        _logger = logger;


    }

    public async Task Handle(AccountCreatedIntegrationEvent affiliateCreated)
    {
        try
        {
            _logger.LogInformation("AccountCreatedIntegrationEventHandler");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create crawler account {event}", affiliateCreated);
            throw;
        }
    }
}

