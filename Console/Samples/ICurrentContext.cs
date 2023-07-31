namespace Console.Samples;

public interface ICurrentContext
{
    string CorrelationId { get; set; }
    string TenantId { get; set; }
}