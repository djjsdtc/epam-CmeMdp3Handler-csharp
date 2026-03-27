using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("field")]
    public class FieldType
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("type")]
        public string Type { get; set; } = "";

        [XmlAttribute("offset")]
        public string? OffsetStr { get; set; }

        [XmlAttribute("refId")]
        public string? RefId { get; set; }

        [XmlAttribute("description")]
        public string? Description { get; set; }

        [XmlAttribute("semanticType")]
        public string? SemanticType { get; set; }

        public long? Offset => OffsetStr != null ? long.Parse(OffsetStr) : null;
    }
}
