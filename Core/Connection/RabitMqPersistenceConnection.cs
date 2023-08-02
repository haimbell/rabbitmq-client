using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace RabbitMq.Client.Core.Connection;

public class RabitMqPersistenceConnection : IRabitMqPersistenceConnection
{
    private readonly ILogger<RabitMqPersistenceConnection> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    bool _disposed = false;

    public RabitMqPersistenceConnection(IOptions<RabbitMqServerOptions> serverOptions, ILogger<RabitMqPersistenceConnection> logger)
    {
        var factory = new ConnectionFactory
        {
            Port = serverOptions.Value.Port.GetValueOrDefault(AmqpTcpEndpoint.UseDefaultPort),
            UserName = serverOptions.Value.UserName,
            Password = serverOptions.Value.Password,
            VirtualHost = serverOptions.Value.VirtualHost,
            ClientProvidedName = serverOptions.Value.ClientProvidedName,
            AutomaticRecoveryEnabled = serverOptions.Value.AutomaticRecoveryEnabled,
            RequestedConnectionTimeout = new TimeSpan(30000),
            RequestedHeartbeat = new TimeSpan(60),
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true,
            DispatchConsumersAsync = true,
            ContinuationTimeout = TimeSpan.FromSeconds(10),
            EndpointResolverFactory = endpoints =>
            {
                string[] hostNames = serverOptions.Value.HostNames;
                return hostNames is { Length: > 0 } ?
                    new DefaultEndpointResolver(serverOptions.Value.HostNames.Select(ep =>
                        new AmqpTcpEndpoint(ep, serverOptions.Value.Port.GetValueOrDefault(-1))))
                    : (IEndpointResolver)new DefaultEndpointResolver(endpoints);
            }
        };

        _connectionFactory = factory;
        _logger = logger;
    }

    public bool IsConnected => !_disposed && (_connection?.IsOpen ?? false);
    object sync_root = new object();
    public bool TryConnect()
    {
        if (IsConnected)
            return true;
        lock (sync_root)
        {
            if (IsConnected)
                return true;
            //use polly to retry connection 5 time with 5 sec delay
            var retryPolicy = Policy.Handle<BrokerUnreachableException>()
                .WaitAndRetry(5, _ => TimeSpan.FromSeconds(5), (ex, time) =>
                {
                    _logger.LogError(ex, "Could not connect to RabbitMQ after {TimeOut}s ({ExceptionMessage})",
                        $"{time.TotalSeconds:n1}", ex.Message);
                });

            retryPolicy.Execute(() =>
            {
                _connection = _connectionFactory.CreateConnection();
                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to events", _connection.Endpoint.HostName);
                _connection.ConnectionShutdown += (_, ea) =>
                {
                    if (_disposed) return;
                    _logger.LogWarning("Connection Shutdown {@Args}", ea);
                    TryConnect();
                };
                _connection.CallbackException += (_, ea) =>
                {
                    if (_disposed) return;
                    _logger.LogWarning(ea.Exception, "Connection CallbackException");
                    TryConnect();
                };
                _connection.ConnectionBlocked += (_, ea) =>
                {
                    if (_disposed) return;
                    _logger.LogWarning("Connection ConnectionBlocked {Reason}", ea.Reason);
                    TryConnect();
                };
                _connection.ConnectionUnblocked += (_, ea) =>
                {
                    if (_disposed) return;
                    _logger.LogWarning("Connection ConnectionUnblocked {Args}", ea);
                    TryConnect();
                };

            });
        }

        if (!IsConnected)
            _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
        return IsConnected;
    }

    public IModel CreateModel()
    {
        TryConnect();

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _disposed = true;
        _connection?.Dispose();
    }
}