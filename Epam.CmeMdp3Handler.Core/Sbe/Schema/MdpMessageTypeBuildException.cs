using System;

namespace Epam.CmeMdp3Handler.Sbe.Schema
{
    public class MdpMessageTypeBuildException : Exception
    {
        public MdpMessageTypeBuildException(string message) : base(message) { }
        public MdpMessageTypeBuildException(string message, Exception inner) : base(message, inner) { }
    }
}
