using RabbitMq.Client.Abstractions;
using System.Text.Json;

namespace RabbitMq.Client.Implementations.Serializers;

public class JsonNetSerializer : IJsonSerializer
{
    public string Serialize(object obj, object? setting = null) =>
        JsonSerializer.Serialize(obj, setting as JsonSerializerOptions);

    public T? Deserialize<T>(string json, object? setting = null) =>
        JsonSerializer.Deserialize<T>(json, setting as JsonSerializerOptions);

    public object? Deserialize(string json, Type type, object? setting = null) =>
        JsonSerializer.Deserialize(json, type, setting as JsonSerializerOptions);
}