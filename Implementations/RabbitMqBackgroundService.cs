//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Routy.Shared.RabbitMq.Implementations.Samples;
//using Routy.Shared.RabbitMq.Implementations.Samples.IMessageHandler;
//using Routy.Shared.RabbitMq.Implementations.Samples.IntegrationEvent;

//namespace Crawler.WebApi.RabbitMq;

//public class RabbitMqBackgroundService : BackgroundService
//{
//    private readonly ILogger<RabbitMqBackgroundService> _logger;
//    private readonly IServiceProvider _provider;
//    private readonly List<RabbitMqConsumer> _consumers = new();

//    public RabbitMqBackgroundService(ILogger<RabbitMqBackgroundService> logger, IServiceProvider provider)
//    {
//        _logger = logger;
//        _provider = provider;
//    }

//    protected override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        try
//        {

//            _logger.LogInformation("RabbitMqBackgroundService is starting.");
//            var scope = _provider.CreateScope();
//            RegisterEvents(scope);


//            for (int i = 0; i < 10; i++)
//            {
//                var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();

//                consumer.Configure((op) =>
//                {
//                    //op.QueueName = "crawler_api_2";
//                    //op.Exchange = "crawler_api_2";
//                    op.ClientProvidedName = "test_1";
//                },( op) =>
//                {
//                    op.QueueName = "crawler_api_2";
//                    op.Exchange = "crawler_api_2";
//                });

//                consumer.StartConsuming(stoppingToken);
//                _consumers.Add(consumer);
//                _logger.LogInformation("RabbitMq consumer is started.");
//            }
//            var producer = scope.ServiceProvider.GetRequiredService<RabbitMqProducer>();
//            for (int i = 0; i < 100; i++)
//            {
//                producer.Publish(new AccountCreatedIntegrationEvent(), "fake_name");
//            }

//            return Task.CompletedTask;
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "RabbitMqBackgroundService threw an exception on startup.");
//            throw;
//        }
//    }

//    private void RegisterEvents(IServiceScope scope)
//    {
       
//        using var consumer = scope.ServiceProvider.GetRequiredService<RabbitMqConsumer>();
//        consumer.AddSubscription<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>
//            ("fake_name");
//        consumer.AddSubscription<AccountCreatedIntegrationEvent, AccountCreatedMessageHandler>
//            (nameof(AccountCreatedIntegrationEvent));
//    }

//    public override void Dispose()
//    {
//        foreach (var consumer in _consumers)
//        {
//            consumer.Dispose();
//        }
//        base.Dispose();
//    }
//}