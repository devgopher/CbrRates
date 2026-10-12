namespace CbrRatesService.Web.Contracts.Responses;

/// <summary>
///     Курс одной валюты в ответе API.
/// </summary>
/// <param name="CharCode">Буквенный код валюты (ISO 4217).</param>
/// <param name="NumCode">Цифровой код валюты (ISO 4217).</param>
/// <param name="Nominal">Номинал.</param>
/// <param name="Name">Наименование валюты.</param>
/// <param name="Value">Значение курса (как у ЦБ РФ).</param>
/// <param name="VunitRate">Курс за единицу (как у ЦБ РФ).</param>
public sealed record CurrencyResponse(
    string CharCode,
    string NumCode,
    int Nominal,
    string Name,
    string Value,
    string VunitRate);
