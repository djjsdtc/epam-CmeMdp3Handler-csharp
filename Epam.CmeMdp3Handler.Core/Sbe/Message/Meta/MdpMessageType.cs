using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /// <summary>
    /// Holder of MDP Message Type definitions.
    /// </summary>
    public class MdpMessageType
    {
        private readonly MessageSchema _schema;
        private readonly MessageType _messageType;
        private readonly SemanticMsgType? _semanticMsgType;
        private readonly IMetadataContainer _metadataContainer;

        /// <summary>
        /// Creates an MDP message type holder and builds its metadata container.
        /// </summary>
        /// <param name="schema">The message schema containing all type definitions</param>
        /// <param name="messageType">The SBE message type definition from the schema</param>
        public MdpMessageType(MessageSchema schema, MessageType messageType)
        {
            _schema = schema;
            _messageType = messageType;
            _semanticMsgType = SemanticMsgTypeExtensions.FromFixValue(messageType.SemanticType);
            _metadataContainer = MetadataContainerBuilder.Build(this);
        }

        /// <summary>Returns the message schema.</summary>
        public MessageSchema GetSchema() => _schema;

        /// <summary>Returns the SBE message type definition.</summary>
        public MessageType GetMessageType() => _messageType;

        /// <summary>Returns the semantic (business) message type, if known.</summary>
        public SemanticMsgType? GetSemanticMsgType() => _semanticMsgType;

        /// <summary>Returns the pre-built metadata container for this message type.</summary>
        public IMetadataContainer GetMetadataContainer() => _metadataContainer;
    }
}
