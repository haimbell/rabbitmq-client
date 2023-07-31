namespace RabbitMq.Client.Abstractions;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RoutingKeyAttribute : Attribute
{
    public string Name { get; }

    public RoutingKeyAttribute(string name)
    {
        Name = name;
    }
}