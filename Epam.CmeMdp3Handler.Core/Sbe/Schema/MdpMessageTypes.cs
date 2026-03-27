using System;
using System.Collections.Generic;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;
using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Schema
{
    public class MdpMessageTypes
    {
        private readonly MdpMessageType?[] _messageTypes;
        private readonly MessageSchema _schema;
        public int MaxIncrBodyLen { get; }

        public MdpMessageTypes(Uri uri)
        {
            try
            {
                _schema = MessageSchemaUnmarshaller.Unmarshall(uri);
            }
            catch (Exception e)
            {
                throw new MdpMessageTypeBuildException("Failed to unmarshall Message Schema: " + e.Message, e);
            }
            _messageTypes = BuildMessageTypes(_schema.Message);
            MaxIncrBodyLen = FindMaxIncrBodyLen();
            if (MaxIncrBodyLen == 0)
                throw new MdpMessageTypeBuildException("Maximum Increment body block length is not found");
        }

        public MdpMessageType?[] GetMessageTypes() => _messageTypes;

        public MdpMessageType? GetMessageType(int id) =>
            id < _messageTypes.Length ? _messageTypes[id] : null;

        private MdpMessageType?[] BuildMessageTypes(List<MessageType> messageTypeList)
        {
            var arr = new MdpMessageType[FindMaxMsgTypeId(messageTypeList) + 1];
            foreach (var mt in messageTypeList)
                arr[mt.Id] = new MdpMessageType(_schema, mt);
            return arr;
        }

        private static int FindMaxMsgTypeId(List<MessageType> list)
        {
            int max = 0;
            foreach (var mt in list)
                if (mt.Id > max) max = mt.Id;
            return max;
        }

        private int FindMaxIncrBodyLen()
        {
            int max = 0;
            foreach (var mt in _messageTypes)
            {
                if (mt != null)
                {
                    int len = ExtractIncrBodyLen(mt);
                    if (len > max) max = len;
                }
            }
            return max;
        }

        private static int ExtractIncrBodyLen(MdpMessageType messageType)
        {
            int max = 0;
            var sem = SemanticMsgTypeExtensions.FromFixValue(messageType.GetMessageType().SemanticType);
            if (sem == SemanticMsgType.MarketDataIncrementalRefresh)
            {
                foreach (var g in messageType.GetMessageType().Group)
                {
                    if (g.Id == MdConstants.NO_MD_ENTRIES && g.BlockLength.HasValue && g.BlockLength.Value > max)
                        max = g.BlockLength.Value;
                }
            }
            return max;
        }
    }
}
