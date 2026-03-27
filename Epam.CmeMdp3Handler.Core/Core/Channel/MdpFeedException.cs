using System;

namespace Epam.CmeMdp3Handler.Core.Channel
{
    public class MdpFeedException : Exception
    {
        public MdpFeedException(string message) : base(message) { }
        public MdpFeedException(string message, Exception inner) : base(message, inner) { }
    }
}
