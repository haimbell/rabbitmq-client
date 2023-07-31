using Newtonsoft.Json;
using RabbitMq.Client.Abstractions;

namespace RabbitMq.Client.Implementations.Serializers;

public class NewtonsoftJsonSerializer : IJsonSerializer
{
    public string Serialize(object obj, object? setting = null) =>
            JsonConvert.SerializeObject(obj, setting as JsonSerializerSettings);

    public T? Deserialize<T>(string json, object? setting = null) =>
        JsonConvert.DeserializeObject<T>(json, setting as JsonSerializerSettings);

    public object Deserialize(string json, Type type, object? setting = null) =>
        JsonConvert.DeserializeObject(json, type, setting as JsonSerializerSettings);
}