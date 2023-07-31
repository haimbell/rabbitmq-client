using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMq.Client.Abstractions.Controllers;
using RabbitMQ.Client.Events;

namespace Console.Samples;

public class MessageController : BrokerControllerBase
{
    private readonly ILogger<MessageController> _logger;
    public ICurrentContext Context => ServiceProvider.GetRequiredService<ICurrentContext>();

    public MessageController(ILogger<MessageController> logger)
    {
        _logger = logger;
    }

    [MessageHandler("exchange1", "default", "test")]
    public Task Handle([Message] AccountCreatedIntegrationEvent accountCreatedIntegrationEvent,
     [FromService] AccountCreatedIntegrationEventHandler handler, BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Handle");
        return Task.CompletedTask;
    }

    [MessageHandler("exchange1", "crawler_api_2", "fake_name")]
    public Task Handle2([Message] AccountCreatedIntegrationEvent accountCreatedIntegrationEvent,
        [FromService] IFakeService handler, BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken = default)
    {
        handler.Do();
        _logger.LogInformation("Handle1 {CorrelationId}", Context?.CorrelationId);
        return Task.CompletedTask;
    }

    [MessageHandler("exchange1", "crawler_api_2", "AccountCreatedIntegrationEvent")]
    public Task Handle2([Message] AccountCreatedIntegrationEvent accountCreatedIntegrationEvent,
        [FromService] AccountCreatedIntegrationEventHandler handler, BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Handle2");
        return Task.CompletedTask;
    }
}