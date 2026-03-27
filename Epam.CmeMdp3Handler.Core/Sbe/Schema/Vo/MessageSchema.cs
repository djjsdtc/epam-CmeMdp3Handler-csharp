using System.Collections.Generic;
using System.Xml.Serialization;

namespace Epam.CmeMdp3Handler.Sbe.Schema.Vo
{
    [XmlRoot("messageSchema")]
    public class MessageSchema
    {
        [XmlElement("types")]
        public List<Types> TypesList { get; set; } = new();

        [XmlElement("message")]
        public List<MessageType> Message { get; set; } = new();

        public class Types
        {
            [XmlElement("type")]
            public List<EncodedDataType> Type { get; set; } = new();

            [XmlElement("composite")]
            public List<CompositeDataType> Composite { get; set; } = new();

            [XmlElement("enum")]
            public List<EnumType> Enum { get; set; } = new();

            [XmlElement("set")]
            public List<SetType> Set { get; set; } = new();
        }
    }
}
