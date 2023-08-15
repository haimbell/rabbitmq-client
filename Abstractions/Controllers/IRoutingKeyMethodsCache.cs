using System.Reflection;

namespace RabbitMq.Client.Abstractions.Controllers;

public interface IRoutingKeyMethodsCache
{
    void Add(MethodInfo methodInfo);
    //void Add(string routingKey, MethodInfo methodInfo, string? queue = null);
    ExcutionMethod? Get(string routingKey, string? queue = null, string? exchange = null);
    IEnumerable<ExcutionMethod> GetAll();
}

public record ExcutionMethod(MethodInfo MethodInfo)
{
    public string Key => $"{Exchange}/{Queue}/{RoutingKey}";
    public string Exchange { get; set; } = null!;
    public string Queue { get; set; } = null!;
    public string RoutingKey { get; set; } = null!;
    public ExcutionMethodParam[] Params { get; set; }
}

public enum ExcutionParamSoruce
{
    Message,
    Service,
    Optional
}
public record ExcutionMethodParam
{
    public ExcutionParamSoruce Source { get; set; }
    public Type Type { get; set; } = null!;
}
