namespace CbrRatesService.Infrastructure.Redis.Settings;

public class RedisCacheSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
}