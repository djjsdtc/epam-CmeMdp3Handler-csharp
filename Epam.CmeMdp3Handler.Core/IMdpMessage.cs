using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to MDP Message.
    /// </summary>
    public interface IMdpMessage : IFieldSet
    {
        /// <summary>Returns the semantic (business) message type, if known.</summary>
        SemanticMsgType? GetSemanticMsgType();

        /// <summary>Sets the message type metadata for this message.</summary>
        /// <param name="messageType">The MDP message type definition</param>
        void SetMessageType(MdpMessageType messageType);
    }
}
