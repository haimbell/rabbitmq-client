using Controllers.Samples;
using Crawler.WebApi.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;

Console.WriteLine("Hello, World!");

var builder = CreateHostBuilder(args);


var build = builder.Build();

build.Services
    .AddRabbitMqControllers(Assembly.GetAssembly(typeof(MessageController)))
    .AddRabbitMqConsumers(1);
var producer = build.Services.GetRequiredService<RabbitMqProducer>();
for (int i = 0; i < 100; i++)
{
    producer.Publish(new AccountCreatedIntegrationEvent(), "fake_name");
    producer.Publish(new { val = "123", count = i }, "fake");
}
build.Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddLogging(_ => _.AddConsole())
                .Configure<RabbitMqConsumerOptions>(_ =>
                {
                    _.Middelwares.Add((serviceProvider, eventArgs) =>
                    {
                        var currentContext = serviceProvider.GetRequiredService<ICurrentContext>();
                        var basicDeliverEventArgs = eventArgs as BasicDeliverEventArgs;
                        currentContext.CorrelationId = basicDeliverEventArgs.BasicProperties.CorrelationId;
                        var tenantId = GetTenantId(basicDeliverEventArgs);
                        currentContext.TenantId = tenantId;
                    });

                })
                .UseRabbitMq(Assembly.GetAssembly(typeof(MessageController)))
                .AddHandlers(Assembly.GetAssembly(typeof(AccountCreatedIntegrationEventHandler)))
                //.AddSingleton<RabbitMqConsumerManager>()
                .AddTransient<RabbitMqConsumer>()
                .AddTransient<RabbitMqProducer>()
                .AddScoped<ICurrentContext, CurrentContext>()
                .AddScoped<IFakeService, FakeService>()

                .Configure<RabbitMqServerOptions>(_ =>
                {
                    _.HostNames = new[] { "localhost" };
                    _.Port = 5672;
                    _.UserName = "guest";
                    _.Password = "guest";
                    _.VirtualHost = "/";
                });
        });


static string GetTenantId(BasicDeliverEventArgs eventArgs)
{
    try
    {
        string tenantId = Guid.Empty.ToString("D");
        if (eventArgs.BasicProperties.IsHeadersPresent()
            && eventArgs.BasicProperties.Headers.ContainsKey("TenantId"))
        {
            var tenantIdByte = (byte[])eventArgs.BasicProperties.Headers["TenantId"];
            if (tenantIdByte != null)
                tenantId = Encoding.UTF8.GetString(tenantIdByte);
        }
        return tenantId;
    }
    catch (Exception e)
    {
        return Guid.Empty.ToString("D");
    }
}

internal class CurrentContext : ICurrentContext
{
    public string CorrelationId { get; set; }
    public string TenantId { get; set; }
}
//var logger = serviceProvider.GetService<ILoggerFactory>()
//    .CreateLogger<Program>();
//logger.LogDebug("Starting application");

