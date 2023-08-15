using Microsoft.Extensions.DependencyInjection;
using RabbitMq.Client.Abstractions;
using RabbitMq.Client.Abstractions.Controllers;
using RabbitMq.Client.Abstractions.MessageHandlers;
using RabbitMq.Client.Core;
using RabbitMq.Client.Core.Connection;
using RabbitMq.Client.Implementations;
using RabbitMq.Client.Implementations.Serializers;
using System.Reflection;

namespace RabbitMq.Client.DependencyInjection;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceProvider AddRabbitMqControllers(this IServiceProvider services, Assembly? assembly = null)
    {
        var keyMethodsCache = services.GetRequiredService<IRoutingKeyMethodsCache>();
        var subscriptionsManager = services.GetRequiredService<IEventBusSubscriptionsManager>();
        //list all types that implement BrokerControllerBase
        assembly ??= Assembly.GetExecutingAssembly();
        var enumerable = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(BrokerControllerBase))
                        && !t.IsAbstract);
        foreach (var type in enumerable)
        {
            //get all public method in type
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var messageHandlerAttribute = method.GetCustomAttribute<MessageHandlerAttribute>();
                if (messageHandlerAttribute == null)
                    continue;
                keyMethodsCache.Add(method);

                subscriptionsManager.AddSubscription(messageHandlerAttribute.Exchange, messageHandlerAttribute.Queue, messageHandlerAttribute.RoutingKey);
            }
        }

        return services;
    }

    public static IServiceProvider AddRabbitMqConsumers(this IServiceProvider services, int consumerPerQueue = 1)
    {
        var routingKeyMethodsCache = services.GetRequiredService<IRoutingKeyMethodsCache>();
        var consumerManager = services.GetRequiredService<RabbitMqConsumerManager>();
        var excutionMethods = routingKeyMethodsCache.GetAll();
        foreach (var excutionMethod in excutionMethods)
        {
            consumerManager.BindQueue(excutionMethod.Exchange, excutionMethod.Queue, excutionMethod.Key);
            consumerManager.AddConsumers(excutionMethod.Exchange, excutionMethod.Queue, consumerPerQueue, excutionMethod.Key);
        }
        return services;
    }
    /// <summary>
    /// Register RabbitMq and all <see cref="BrokerControllerBase"/>
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assembly">assembly that contains all <see cref="BrokerControllerBase"/> </param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection UseRabbitMq(this IServiceCollection services, Assembly? assembly = null, Action<RabbitMqServerOptions> options = null)
    {
        services.AddSingleton<IRoutingKeyMethodsCache, RoutingKeyMethodsCache>()
            .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>()
            .AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>()
            .AddTransient<IRabitMqPersistenceConnection, RabitMqPersistenceConnection>()
            .AddSingleton<RabbitMqConsumerManager>()
            .AddTransient<RabbitMqConsumer>()
            .AddTransient<RabbitMqProducer>();

        //list all types that implement BrokerControllerBase
        assembly ??= Assembly.GetExecutingAssembly();
        var enumerable = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(BrokerControllerBase))
                                   && !t.IsAbstract);

        foreach (var type in enumerable)
        {

            //get all public method in type
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                //get all parameters in method
                var parameters = method.GetParameters();
                foreach (var parameter in parameters)
                {
                    //get all attributes in parameter
                    var attributes = parameter.GetCustomAttributes();
                    foreach (var attribute in attributes)
                    {
                        //if attribute is MessageAttribute
                        if (attribute is MessageAttribute messageAttribute)
                        {
                            //get the type of T in interface IMessageHandler<T>
                            var messageType = parameter.ParameterType;
                            //create a IMessageHandler<T> using messageType
                            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
                            //services.AddTransient(type);
                            services.AddTransient(handlerType, type);
                        }
                    }
                }
            }
            //register the controller as service
            services.AddTransient(type);

        }
        return services;
    }

    /// <summary>
    /// Register all handlers in the executing assembly
    /// </summary>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services)
    {
        return services.AddMessageHandlers(Assembly.GetExecutingAssembly())
            .AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
    }

    /// <summary>
    /// Register all handlers in the given assembly
    /// </summary>
    /// <param name="assembly"> assembly that contains handlers </param>
    public static IServiceCollection AddMessageHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        var enumerable = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IMessageHandler))
                        && !t.IsAbstract);
        //register all types as service
        foreach (var type in enumerable)
        {
            //get the type of T in interface IMessageHandler<T>
            var messageType = type.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Select(t => t.GetGenericArguments()[0]).FirstOrDefault();
            if (messageType == null)
                continue;
            //create a IMessageHandler<T> using messageType
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
            services.AddTransient(type);
            services.AddTransient(handlerType, type);

        }
        return services;
    }
}