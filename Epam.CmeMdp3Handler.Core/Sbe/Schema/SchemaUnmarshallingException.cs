using System;

namespace Epam.CmeMdp3Handler.Sbe.Schema
{
    public class SchemaUnmarshallingException : Exception
    {
        public SchemaUnmarshallingException(string message) : base(message) { }
        public SchemaUnmarshallingException(string message, Exception inner) : base(message, inner) { }
    }
}
