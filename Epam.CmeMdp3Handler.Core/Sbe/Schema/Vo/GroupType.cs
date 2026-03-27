using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("group")]
    public class GroupType
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("blockLength")]
        public string? BlockLengthStr { get; set; }

        [XmlAttribute("dimensionType")]
        public string DimensionType { get; set; } = "";

        [XmlElement("field")]
        public List<FieldType> Field { get; set; } = new();

        public int? BlockLength => BlockLengthStr != null ? int.Parse(BlockLengthStr) : null;
    }
}
