using System.Xml.Serialization;

namespace CbrRatesService.Models;

[XmlRoot("ValCurs")]
public class CurrencyRate
{
    [XmlAttribute("Date")] public string Date { get; set; } = null!;

    [XmlAttribute("name")] public string Name { get; set; } = null!;

    [XmlElement("Valute")] public List<Currency> Currencies { get; set; } = [];
}