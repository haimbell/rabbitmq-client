using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Core;

namespace RabbitMq.Client.Implementations;

public class RabbitMqConsumerManager : IDisposable
{
    private readonly ILogger<RabbitMqConsumerManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly List<RabbitMqConsumer> _consumers = new();

    public RabbitMqConsumerManager(ILogger<RabbitMqConsumerManager> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    public Task BindQueue(string exchange, string queue, string? clientName = null)
    {
        try
        {
            var scope = _provider.CreateScope();
            clientName ??= $"Migration_{exchange}_{queue}";
            var eventBusSubscriptionsManager = scope.ServiceProvider.GetRequiredService<IEventBusSubscriptionsManager>();
            var routingKeys = eventBusSubscriptionsManager.GetRoutingKeys(exchange, queue).ToList();
            if (routingKeys is { Count: > 0 })
            {
                using var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();
                consumer.Configure((op) =>
                {
                    op.ClientProvidedName = clientName;
                }, (op) =>
                {
                    op.QueueName = queue;
                    op.Exchange = exchange;
                });
                consumer.Migrate();
                foreach (var routingKey in routingKeys)
                {
                    consumer.AddSubscription(routingKey);
                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to subscribe to queue");
            throw;
        }
        return Task.CompletedTask;
    }

    public Task AddConsumers(string exchange, string queue, int consumersCount = 1, string? clientName = null, CancellationToken stoppingToken = default)
    {
        try
        {
            _logger.LogInformation("RabbitMqConsumerManager is starting.");
            var scope = _provider.CreateScope();
            clientName ??= $"NONE_{exchange}_{queue}";

            for (int i = 0; i < consumersCount; i++)
            {
                var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();
                consumer.Configure((op) =>
                {
                    op.ClientProvidedName = clientName;
                }, (op) =>
                {
                    op.Exchange = exchange;
                    op.QueueName = queue;
                });

                consumer.StartConsuming(stoppingToken);
                _consumers.Add(consumer);
                _logger.LogInformation("RabbitMq consumer is started.");
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMqBackgroundService threw an exception on startup.");
            throw;
        }
    }



    public void Dispose()
    {
        foreach (var consumer in _consumers)
        {
            consumer.Dispose();
        }
    }
}