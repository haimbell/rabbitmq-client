using System.Reflection;
using System.Text;
using Console;
using Console.Samples;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMq.Client.Core;
using RabbitMq.Client.DependencyInjection;
using RabbitMQ.Client.Events;

System.Console.WriteLine("Hello, World!");

var builder = CreateHostBuilder(args);


var build = builder.Build();

build.Services
    .AddRabbitMqControllers(Assembly.GetAssembly(typeof(MessageController)))
    .AddRabbitMqConsumers(1);
var producer = build.Services.GetRequiredService<RabbitMqProducer>();
for (int i = 0; i < 100; i++)
{
    //producer.Publish(new AccountCreatedIntegrationEvent(), exchange: "exchange1");
    //producer.Publish(new AccountCreatedIntegrationEvent(), "fake_name", exchange: "exchange1");
    producer.Publish(new { val = "123", count = i }, "dynamic_rk", exchange: "exchange1");
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
                .AddEventHandlers(Assembly.GetAssembly(typeof(AccountCreatedIntegrationEventHandler)))
                .AddMessageHandlers(Assembly.GetAssembly(typeof(AccountCreatedIntegrationEventHandler)))
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

namespace Console
{
    internal class CurrentContext : ICurrentContext
    {
        public string CorrelationId { get; set; }
        public string TenantId { get; set; }
    }
}
