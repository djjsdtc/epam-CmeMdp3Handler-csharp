using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("message")]
    public class MessageType
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("semanticType")]
        public string? SemanticType { get; set; }

        [XmlAttribute("blockLength")]
        public string? BlockLengthStr { get; set; }

        [XmlElement("field")]
        public List<FieldType> Field { get; set; } = new();

        [XmlElement("group")]
        public List<GroupType> Group { get; set; } = new();

        public int? BlockLength => BlockLengthStr != null ? int.Parse(BlockLengthStr) : null;
    }
}
