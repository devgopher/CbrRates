namespace CbrRatesService.Infrastructure.Abstractions.Cache.Settings;

public class CacheSettings<T>
{
    public const string SectionName = "CacheSettings";

    public bool? Enabled { get; set; }

    public TimeSpan? DefaultExpiration { get; set; }

    public T? EngineSettings { get; set; }
}