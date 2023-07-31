using System.Reflection;
using System.Runtime.InteropServices;
using RabbitMq.Client.Abstractions.Controllers;

namespace RabbitMq.Client.Implementations;

public class RoutingKeyMethodsCache : IRoutingKeyMethodsCache
{
    private readonly Dictionary<string, ExcutionMethod> _methods = new();

    public void Add(MethodInfo methodInfo)
    {
        ExcutionMethod excutionMethod = new(methodInfo);
        List<ExcutionMethodParam> excutionMethodParams = new();
        var messageHandlerAttribute = methodInfo.GetCustomAttribute<MessageHandlerAttribute>();
        if (messageHandlerAttribute == null)
            throw new Exception($"method {methodInfo.Name} must have MessageHandlerAttribute");
        //get all parameters in method
        excutionMethod.Exchange = messageHandlerAttribute.Exchange;
        excutionMethod.Queue = messageHandlerAttribute.Queue;
        excutionMethod.RoutingKey = messageHandlerAttribute.RoutingKey;
        var parameters = methodInfo.GetParameters();
        foreach (var parameter in parameters)
        {
            //get all attributes in parameter
            if (parameter.CustomAttributes.Any())
            {
                var attributes = parameter.GetCustomAttributes();
                foreach (var attribute in attributes)
                {
                    //if attribute is MessageAttribute
                    if (attribute is MessageAttribute)
                    {
                        excutionMethodParams.Add(new() { Source = ExcutionParamSoruce.Message, Type = parameter.ParameterType });
                    }
                    else if (attribute is FromServiceAttribute)
                    {
                        excutionMethodParams.Add(new() { Source = ExcutionParamSoruce.Service, Type = parameter.ParameterType });
                    }
                    else if (attribute is OptionalAttribute)
                    {
                        excutionMethodParams.Add(new() { Source = ExcutionParamSoruce.Optional, Type = parameter.ParameterType });
                    }
                    else
                    {
                        throw new Exception($"parameter {parameter.Name} must have MessageAttribute or FromServiceAttribute");
                    }
                }
            }
            else
            {
                excutionMethodParams.Add(new() { Source = ExcutionParamSoruce.Optional, Type = parameter.ParameterType });
            }
        }
        excutionMethod.Params = excutionMethodParams.ToArray();
        if (_methods.TryGetValue(excutionMethod.Key, out ExcutionMethod? method))
        {
            throw new ArgumentException(
                $"RoutingKey '{method.RoutingKey}' of Queue '{method.Queue}' is already handled by '{method.MethodInfo.Name}' from '{method.MethodInfo.DeclaringType?.Name}' ")
            {
                Data =
                {
                    { "trigger", excutionMethod },
                    { "handler", method }
                }
            };
        }
        _methods.Add(excutionMethod.Key, excutionMethod);
    }


    public ExcutionMethod? Get(string routingKey, string? queue = null, string? exchange = null) =>
        _methods.TryGetValue($"{exchange}/{queue}/{routingKey}", out var methodInfo) ? methodInfo : null;

    public IEnumerable<ExcutionMethod> GetAll() => _methods.Values;
}