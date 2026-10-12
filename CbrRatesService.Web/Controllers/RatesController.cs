using CbrRatesService.Application.Services;
using CbrRatesService.Web.Contracts.Requests;
using CbrRatesService.Web.Contracts.Responses;
using CbrRatesService.Web.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace CbrRatesService.Web.Controllers;

/// <summary>
///     API курсов валют (<c>/api/v1/rates</c>).
/// </summary>
[ApiController]
[Route("api/v1/rates")]
[Produces("application/json")]
public sealed class RatesController(IRatesService ratesService) : ControllerBase
{
    /// <summary>
    ///     Возвращает курсы ЦБ РФ на дату (или на сегодня).
    ///     Если задан <c>currency</c> и валюта не найдена — HTTP 204.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(RatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RatesResponse>> GetRates(
        [FromQuery] GetRatesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.HasInvalidDate)
        {
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Некорректный формат даты. Используйте yyyy-MM-dd или dd.MM.yyyy.",
                Instance = HttpContext.Request.Path
            });
        }

        var rates = await ratesService
            .GetRatesAsync(request.ParsedDate, request.Currency, cancellationToken)
            .ConfigureAwait(false);

        return rates is null
            ? NoContent()
            : Ok(RatesResponseMapper.ToResponse(rates, request.ParsedDate));
    }
}
