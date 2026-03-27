using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("composite")]
    public class CompositeDataType
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlElement("type")]
        public List<EncodedDataType> Type { get; set; } = new();
    }
}
