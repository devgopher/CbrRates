using System.Xml.Serialization;

namespace CbrRatesService.Models;

public class Currency
{
    [XmlAttribute("ID")] public string Id { get; set; } = null!;

    [XmlElement("NumCode")] public string NumCode { get; set; } = null!;

    [XmlElement("CharCode")] public string CharCode { get; set; } = null!;

    [XmlElement("Nominal")] public int Nominal { get; set; }

    [XmlElement("Name")] public string Name { get; set; } = null!;

    [XmlElement("Value")] public string Value { get; set; } = null!;

    [XmlElement("VunitRate")] public string VunitRate { get; set; } = null!;
}