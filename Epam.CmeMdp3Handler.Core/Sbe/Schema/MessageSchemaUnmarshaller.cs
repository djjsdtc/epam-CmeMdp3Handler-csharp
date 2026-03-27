using System;
using System.IO;
using System.Xml.Serialization;
using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Schema
{
    public static class MessageSchemaUnmarshaller
    {
        private static readonly XmlSerializer _serializer = new(typeof(MessageSchema));

        public static MessageSchema Unmarshall(Uri uri)
        {
            try
            {
                using var stream = new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read);
                return Unmarshall(stream);
            }
            catch (SchemaUnmarshallingException) { throw; }
            catch (Exception e)
            {
                throw new SchemaUnmarshallingException("Failed to parse MDP Schema: " + e.Message, e);
            }
        }

        public static MessageSchema Unmarshall(Stream inputStream)
        {
            try
            {
                /* Schema has broken namespaces so namespace filter is used to map namespaces */
                using var reader = new StreamReader(inputStream);
                using var xmlReader = new NamespaceStrippingXmlReader(reader);
                var result = _serializer.Deserialize(xmlReader);
                if (result == null)
                    throw new SchemaUnmarshallingException("Deserialization returned null");
                return (MessageSchema)result;
            }
            catch (SchemaUnmarshallingException) { throw; }
            catch (Exception e)
            {
                throw new SchemaUnmarshallingException("Failed to parse MDP Schema: " + e.Message, e);
            }
        }
    }
}
