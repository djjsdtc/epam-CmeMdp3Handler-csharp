using System.Xml;

namespace Epam.CmeMdp3Handler.Sbe.Schema
{
    // Strips XML namespace so XmlSerializer can deserialize the SBE schema
    // which has namespace "http://www.fixprotocol.org/ns/simple/1.0"
    // without requiring all VO classes to be annotated with that namespace.
    internal class NamespaceStrippingXmlReader : XmlTextReader
    {
        public NamespaceStrippingXmlReader(System.IO.TextReader reader) : base(reader) { }

        public override string NamespaceURI => "";
    }
}
