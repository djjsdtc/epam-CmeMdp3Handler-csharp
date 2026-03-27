using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("validValue")]
    public class ValidValue
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlText]
        public string Value { get; set; } = "";
    }
}
