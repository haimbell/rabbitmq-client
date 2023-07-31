using Crawler.WebApi.RabbitMq;
using Microsoft.Extensions.Logging;

namespace Controllers.Samples;

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

