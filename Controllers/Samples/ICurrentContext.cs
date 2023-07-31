namespace Crawler.WebApi.RabbitMq;

public interface ICurrentContext
{
    string CorrelationId { get; set; }
    string TenantId { get; set; }
}