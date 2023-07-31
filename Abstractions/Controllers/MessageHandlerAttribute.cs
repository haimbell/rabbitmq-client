namespace Controllers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MessageHandlerAttribute : Attribute
{
    public string Exchange { get; init; }
    public string Queue { get; init; }
    public string RoutingKey { get; init; }
    public MessageHandlerAttribute(string exchange, string queue, string routingKey)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
        Queue = queue;
    }
}