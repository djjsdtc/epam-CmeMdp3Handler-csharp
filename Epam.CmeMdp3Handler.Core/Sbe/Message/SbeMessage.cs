using System;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeMessage : AbstractFieldSet, IMdpMessage
    {
        private MdpMessageType? _messageType;

        public SbeMessage()
        {
            _sbeBuffer = new SbeBufferImpl();
        }

        public int GetMsgSize()
        {
            Buffer().Position(SbeConstants.MSG_SIZE_OFFSET);
            return Buffer().GetUInt16();
        }

        public int GetBlockLength()
        {
            Buffer().Position(SbeConstants.BLOCK_LENGTH_OFFSET);
            return Buffer().GetUInt16();
        }

        public override int GetSchemaId()
        {
            Buffer().Position(SbeConstants.TEMPLATE_ID_OFFSET);
            return Buffer().GetUInt16();
        }

        public int GetVersion()
        {
            Buffer().Position(SbeConstants.VERSION_OFFSET);
            return Buffer().GetUInt8();
        }

        public MdpMessageType? GetMessageType() => _messageType;

        public void SetMessageType(MdpMessageType messageType) { _messageType = messageType; }

        public SemanticMsgType? GetSemanticMsgType() =>
            _messageType != null ? _messageType.GetSemanticMsgType() : null;

        public override IMetadataContainer Metadata() => _messageType!.GetMetadataContainer();

        protected override void Seek(int tagId) => Seek(Metadata().FindField(tagId)!);

        protected override void Seek(SbeFieldType field) => field.Seek(_sbeBuffer!);

        public override bool GetGroup(int tagId, IMdpGroup mdpGroup)
        {
            var mutableGroup = (IMutableMdpGroup)mdpGroup;

            int groupOffset = SbeConstants.HEADER_SIZE + GetBlockLength();

            var groups = _messageType!.GetMetadataContainer().AllGroups();
            if (groups == null) return false;

            foreach (var groupType in groups)
            {
                if (groupType == null) continue;
                Buffer().Position(groupOffset);
                int blockLength = Buffer().GetUInt16();
                Buffer().Position(groupOffset + groupType.NumInGroupOffset);
                int numInGroup = Buffer().GetUInt8();

                if (groupType.GetGroupType().Id == tagId)
                {
                    mutableGroup.Reset(Buffer(), groupType, blockLength, numInGroup, groupOffset + groupType.DimensionBlockLength);
                    return true;
                }
                else
                {
                    groupOffset = groupOffset + groupType.DimensionBlockLength + blockLength * numInGroup;
                }
            }
            return false;
        }

        public override IFieldSet Copy()
        {
            var copy = new SbeMessage();
            copy.Buffer().CopyFrom(Buffer());
            copy._messageType = _messageType;
            return copy;
        }

        public override string ToString() =>
            $"MDPMessage{{schemaId='{GetSchemaId()}', blockLength='{GetBlockLength()}', msgSize='{GetMsgSize()}'}}";
    }
}
