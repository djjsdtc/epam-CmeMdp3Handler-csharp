using System.Collections.Generic;
using System.Threading;
using Epam.CmeMdp3Handler.Control;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MktData;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.Channel
{
    public class ChannelInstruments : IMdpFeedListener
    {
        private const int SecIdTag = 48;
        private const int SecDescTag = 55;
        private const int SecMdFeedTypes = 1141;
        private const int SecMdFeedType = 1022;
        private const int SecMarketDepth = 264;
        private const byte SecDefaultMdDepth = 10;

        private const int PrcdMsgCountNull = int.MaxValue; // max value used as undefined (null)
        private const int InstrumentCyclesMax = 2; // do we need an option in configuration for this?

        private readonly ChannelContext _channelContext;
        private readonly Dictionary<int, InstrumentController> _instruments = new();
        private int _msgCountDown = PrcdMsgCountNull;

        private readonly SbeString _secDescString = SbeString.Allocate(100);
        private readonly IMdpGroup _mdTypeGroup = SbeGroup.Instance();
        private readonly SbeString _secMdFeedType = SbeString.Allocate(3);

        public ChannelInstruments(ChannelContext channelContext)
        {
            _channelContext = channelContext;
        }

        public void OnFeedStarted(FeedType feedType, Feed feed) { } // nothing yet
        public void OnFeedStopped(FeedType feedType, Feed feed) { } // nothing yet

        public void OnMessage(MdpFeedContext feedContext, IMdpMessage secDefMsg)
        {
            int subscriptionFlags = _channelContext.NotifySecurityDefinitionListeners(secDefMsg);
            byte depth = ExtractMaxDepthFromSecDef(secDefMsg);
            if (!MdEventFlags.IsNothing(subscriptionFlags))
            {
                RegisterSecurity(secDefMsg, subscriptionFlags, depth);
            }
            else
            {
                int securityId = secDefMsg.GetInt32(SecIdTag);
                var strValObj = feedContext.GetStrValObj();
                bool hasValue = secDefMsg.GetString(SecDescTag, strValObj);
                if (hasValue)
                    UpdateSecDesc(securityId, strValObj.GetString());
            }

            if (Volatile.Read(ref _msgCountDown) == PrcdMsgCountNull)
            {
                int totalNumReports = GetTotalReportNum(secDefMsg) * InstrumentCyclesMax;
                Interlocked.CompareExchange(ref _msgCountDown, totalNumReports, PrcdMsgCountNull);
            }
            int msgLeft = Interlocked.Decrement(ref _msgCountDown);
            if (CanStopInstrumentListening(msgLeft))
                _channelContext.StopInstrumentFeeds();
        }

        public void OnPacket(MdpFeedContext feedContext, MdpPacket instrumentPacket)
        {
            foreach (var mdpMessage in instrumentPacket)
            {
                var messageType = _channelContext.GetMdpMessageTypes().GetMessageType(mdpMessage.GetSchemaId())!;
                var semanticMsgType = messageType.GetSemanticMsgType();
                if (semanticMsgType == SemanticMsgType.SecurityDefinition)
                    mdpMessage.SetMessageType(messageType);
                OnMessage(feedContext, mdpMessage);
            }
        }

        private static bool CanStopInstrumentListening(int cyclesLeft) => cyclesLeft <= 0;

        public InstrumentController? Find(int securityId)
            => Find(securityId, null, false, 0, 0);

        public InstrumentController? Find(int securityId, string? secDesc, bool createIfAbsent,
            int subscriptionFlags, byte maxDepth)
        {
            if (_instruments.TryGetValue(securityId, out var controller))
                return controller;
            if (createIfAbsent)
                RegisterSecurity(securityId, secDesc, subscriptionFlags, maxDepth);
            return _instruments.TryGetValue(securityId, out controller) ? controller : null;
        }

        private void RegisterController(int securityId, string? secDesc, int subscriptionFlags,
            byte maxDepth, int gapThreshold)
        {
            _instruments[securityId] = new InstrumentController(_channelContext, securityId, secDesc,
                subscriptionFlags, maxDepth, gapThreshold);
        }

        public void RegisterSecurity(IMdpMessage secDef, int subscriptionFlags, byte maxDepth)
        {
            int securityId = secDef.GetInt32(SecIdTag);
            secDef.GetString(SecDescTag, _secDescString);
            RegisterSecurity(securityId, _secDescString.GetString(), subscriptionFlags, maxDepth);
        }

        public void UpdateSecDesc(int securityId, string secDesc)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                controller?.SetSecDesc(secDesc);
            }
        }

        public bool RegisterSecurity(int securityId, string? secDesc, int subscriptionFlags, byte maxDepth)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                if (controller != null)
                    return controller.OnResubscribe(subscriptionFlags);
                RegisterController(securityId, secDesc, subscriptionFlags, maxDepth, _channelContext.GetGapThreshold());
                return true;
            }
        }

        public void ResetAll()
        {
            foreach (var kv in _instruments)
                kv.Value.OnChannelReset();
        }

        private static int GetTotalReportNum(IMdpMessage mdpMessage) => (int)mdpMessage.GetUInt32(911);

        public void ResetCycleCounter() => Volatile.Write(ref _msgCountDown, PrcdMsgCountNull);

        public void DiscontinueSecurity(int securityId)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                controller?.Discontinue();
            }
        }

        public int GetSubscriptionFlags(int securityId)
        {
            var controller = Find(securityId);
            return controller?.GetSubscriptionFlags() ?? MdEventFlags.NOTHING;
        }

        public void SetSubscriptionFlags(int securityId, int flags, byte maxDepth)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                if (controller != null)
                    controller.SetSubscriptionFlags(flags);
                else
                    RegisterController(securityId, null, flags, maxDepth, _channelContext.GetGapThreshold());
            }
        }

        public void AddSubscriptionFlags(int securityId, int flags, byte maxDepth)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                if (controller != null)
                    controller.AddSubscriptionFlag(flags);
                else
                    RegisterController(securityId, null, flags, maxDepth, _channelContext.GetGapThreshold());
            }
        }

        public void RemoveSubscriptionFlags(int securityId, int flags)
        {
            lock (_instruments)
            {
                var controller = Find(securityId);
                controller?.RemoveSubscriptionFlag(flags);
            }
        }

        private byte ExtractMaxDepthFromSecDef(IMdpMessage secDef)
        {
            secDef.GetGroup(SecMdFeedTypes, _mdTypeGroup);
            while (_mdTypeGroup.HasNext())
            {
                _mdTypeGroup.Next();
                _mdTypeGroup.GetString(SecMdFeedType, _secMdFeedType);
                if (_secMdFeedType.GetCharAt(_secMdFeedType.GetLength() - 1) == 'X')
                    return _mdTypeGroup.GetInt8(SecMarketDepth);
            }
            return SecDefaultMdDepth;
        }
    }
}
