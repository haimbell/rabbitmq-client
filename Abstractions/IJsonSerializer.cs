namespace RabbitMq.Client.Abstractions;

public interface IJsonSerializer
{
    string Serialize(object obj, object? setting = null);
    T? Deserialize<T>(string json, object? setting = null);
    object? Deserialize(string json, Type type, object? setting = null);
}