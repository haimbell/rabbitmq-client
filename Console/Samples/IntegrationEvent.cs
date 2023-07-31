using Newtonsoft.Json;
using RabbitMq.Client.Abstractions.IntegrationEvents;

namespace Console.Samples;

/// <summary>
/// Backwards compatibility
/// </summary>
[Obsolete]
public class IntegrationEvent: IIntegrationEvent
{
    public IntegrationEvent() :
        this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }

    public IntegrationEvent(string tenantId) :
        this(Guid.NewGuid(), DateTime.UtcNow)
    {
        Headers = new Dictionary<string, object>()
        {
            { "TenantId", tenantId }
        };
    }

    [System.Text.Json.Serialization.JsonConstructor]
    public IntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }

    [JsonProperty] public Guid Id { get; private set; }
    [JsonProperty] public DateTime CreationDate { get; private set; }
    [System.Text.Json.Serialization.JsonIgnore] public Dictionary<string, object> Headers { get; set; }

}