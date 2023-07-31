using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMq.Client.Core;

namespace RabbitMq.Client.Implementations;

public class RabbitMqConsumerManager : IDisposable
{
    private readonly ILogger<RabbitMqConsumerManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly Dictionary<string, List<RabbitMqConsumer>> _consumers = new();

    public RabbitMqConsumerManager(ILogger<RabbitMqConsumerManager> logger, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
    }

    public Task AddConsumers(string exchange, string queue, int consumersCount = 1, string? clientName = null, CancellationToken stoppingToken = default)
    {
        try
        {
            _logger.LogInformation("RabbitMqConsumerManager is starting.");
            var scope = _provider.CreateScope();
            clientName ??= $"NONE_{exchange}_{queue}";
            RegisterEvents(scope, exchange, queue, clientName);
            for (int i = 0; i < consumersCount; i++)
            {
                var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();
                var queueId = $"{exchange}_{queue}";
                if (!_consumers.ContainsKey(queueId))
                    _consumers.Add(queueId, new List<RabbitMqConsumer>());
                consumer.Configure((op) =>
                {
                    op.ClientProvidedName = clientName;
                }, (op) =>
                {
                    op.Exchange = exchange;
                    op.QueueName = queue;
                });

                consumer.StartConsuming(stoppingToken);
                _consumers[queueId].Add(consumer);
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


    private void RegisterEvents(IServiceScope scope, string exchange, string queue, string clientName)
    {
        try
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
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to subscribe to queue");
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var consumer in _consumers.Values.SelectMany(consumers => consumers))
        {
            consumer.Dispose();
        }
    }
}