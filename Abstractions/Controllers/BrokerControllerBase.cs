namespace RabbitMq.Client.Abstractions.Controllers;

public class BrokerControllerBase
{
    public IServiceProvider ServiceProvider { get; private set; }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}