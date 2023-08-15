using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Abstractions.IntegrationEvents;
using RabbitMq.Client.Abstractions.MessageHandlers;
using RabbitMq.Client.Implementations;

namespace Console.Samples;

public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>
    /// Register all handlers in the executing assembly
    /// </summary>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        return services.AddEventHandlers(Assembly.GetExecutingAssembly())
            .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
    }

    /// <summary>
    /// Register all handlers in the given assembly
    /// </summary>
    /// <param name="assembly"> assembly that contains handlers </param>
    public static IServiceCollection AddEventHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        var enumerable = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IIntegrationEventHandler))
                        && !t.IsAbstract);
        //register all types as service
        foreach (var type in enumerable)
        {
            var genericHandledType = type.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(t => t.GetGenericArguments()[0]).FirstOrDefault();
            if (genericHandledType != null)
            {
                var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(genericHandledType);
                services.AddTransient(type);
                services.AddTransient(handlerType, type);

            }
          
        }
        return services;
    }
}