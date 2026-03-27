using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("set")]
    public class SetType
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("encodingType")]
        public string EncodingType { get; set; } = "";

        [XmlElement("choice")]
        public List<Choice> Choice { get; set; } = new();
    }
}
