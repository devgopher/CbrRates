using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CbrRatesService.Web.Contracts.Requests;

/// <summary>
///     Query-параметры для <c>GET /api/v1/rates</c>.
/// </summary>
public sealed class GetRatesRequest
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy"];

    /// <summary>
    ///     Дата курса (<c>yyyy-MM-dd</c> или <c>dd.MM.yyyy</c>). Если не указана — текущая дата.
    /// </summary>
    [FromQuery(Name = "date")]
    public string? Date { get; init; }

    /// <summary>
    ///     Код валюты (например, USD). Если не указан — возвращаются все валюты.
    /// </summary>
    [FromQuery(Name = "currency")]
    public string? Currency { get; init; }

    /// <summary>
    ///     Признак некорректного формата <c>date</c> (параметр передан, но не разобран).
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public bool HasInvalidDate =>
        !string.IsNullOrWhiteSpace(Date) && ParsedDate is null;

    /// <summary>
    ///     Разобранная дата курса или <c>null</c>, если дата не указана/некорректна.
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public DateOnly? ParsedDate => TryParseDate(Date);

    private static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateOnly.TryParseExact(
                value,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exact))
            return exact;

        return null;
    }
}
