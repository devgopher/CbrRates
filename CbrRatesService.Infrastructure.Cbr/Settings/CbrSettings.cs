namespace CbrRatesService.Infrastructure.Cbr.Settings;

/// <summary>
///     Настройки интеграции с API ЦБ РФ.
/// </summary>
public sealed class CbrSettings
{
    public const string SectionName = "Cbr";

    /// <summary>
    ///     Базовый URL API ЦБ (например, https://www.cbr.ru).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Таймаут запроса к API ЦБ в секундах.
    /// </summary>
    public int Timeout { get; set; } = 30;

    /// <summary>
    ///     Параметры повторных попыток при ошибках запроса.
    /// </summary>
    public RetrySettings Retry { get; set; } = new();
}

/// <summary>
///     Параметры retry для запросов к ЦБ.
/// </summary>
public sealed class RetrySettings
{
    /// <summary>
    ///     Задержка между попытками.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     Максимальное число повторных попыток.
    /// </summary>
    public int Retries { get; set; } = 3;
}