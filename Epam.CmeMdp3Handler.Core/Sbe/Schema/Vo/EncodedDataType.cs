using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlType("type")]
    public class EncodedDataType
    {
        [XmlAttribute("name")]
        public string Name { get; set; } = "";

        [XmlAttribute("primitiveType")]
        public string PrimitiveType { get; set; } = "";

        [XmlAttribute("presence")]
        public string? Presence { get; set; }

        [XmlAttribute("nullValue")]
        public string? NullValue { get; set; }

        [XmlText]
        public string? Value { get; set; }

        [XmlAttribute("length")]
        public string? LengthStr { get; set; }

        [XmlAttribute("offset")]
        public int Offset { get; set; }

        public int? Length => LengthStr != null ? int.Parse(LengthStr) : null;
    }
}
