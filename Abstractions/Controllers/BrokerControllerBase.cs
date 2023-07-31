using Crawler.WebApi.RabbitMq;

namespace Controllers;

public class BrokerControllerBase
{
    public IServiceProvider ServiceProvider { get; private set; }
    public ICurrentContext? Context => (ICurrentContext?)ServiceProvider.GetService(typeof(ICurrentContext));

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}