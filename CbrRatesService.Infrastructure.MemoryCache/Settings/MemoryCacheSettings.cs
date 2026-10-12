namespace CbrRatesService.Infrastructure.MemoryCache.Settings;

public class MemoryCacheSettings
{
    public const string SectionName = "Memory";

    public TimeSpan? ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);
}