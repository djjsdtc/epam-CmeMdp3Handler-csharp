using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("enum")]
    public class EnumType
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("encodingType")]
        public string EncodingType { get; set; } = "";

        [XmlElement("validValue")]
        public List<ValidValue> ValidValue { get; set; } = new();
    }
}
