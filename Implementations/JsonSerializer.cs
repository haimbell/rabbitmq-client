using Newtonsoft.Json;

namespace Crawler.WebApi.RabbitMq;

public class JsonSerializer : IJsonSerializer
{
    public string Serialize(object obj, object setting = null)
    {
        return JsonConvert.SerializeObject(obj, setting as JsonSerializerSettings);

    }

    public T? Deserialize<T>(string json, object setting = null)
    {
        return JsonConvert.DeserializeObject<T>(json, setting as JsonSerializerSettings);
    }

    public object Deserialize(string json, Type type, object setting = null)
    {
        return JsonConvert.DeserializeObject(json, type, setting as JsonSerializerSettings);
    }
}