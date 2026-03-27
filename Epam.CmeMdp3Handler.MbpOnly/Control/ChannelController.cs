using System.Collections.Generic;
using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MktData;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Microsoft.Extensions.Logging;
using static Epam.CmeMdp3Handler.MktData.MdConstants;

namespace Epam.CmeMdp3Handler.Control
{
    public class ChannelController
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ChannelController>();

        private readonly ChannelContext _channelContext;
        private static readonly int PrcdSnptCountNull = (int)SbePrimitiveType.Int32.NullValue;
        private const int SnapshotCyclesMax = 5;

        private readonly MdpMessageTypes _mdpMessageTypes;
        private readonly RequestForQuoteHandler _requestForQuoteHandler;
        private readonly SecurityStatusHandler _securityStatusHandler;
        private int _snptMsgCountDown = PrcdSnptCountNull;

        private ChannelState _state = ChannelState.INITIAL;
        private long _prcdSeqNum = 0;
        private long _lastMsgSeqNumPrcd369 = 0;
        private readonly object _lock = new object();

        private long _lastIncrPcktReceived = 0;
        private bool _wasChannelResetInPrcdPacket = false;

        private readonly IEventController _eventController = new InMemoryEventController();
        private readonly HashSet<int> _outOfSyncInstruments = new HashSet<int>();

        private readonly EventCommitFunction _eventCommitFunction;

        public ChannelController(ChannelContext channelContext, int queueSize, int queueSlotBufSize)
        {
            _mdpMessageTypes = channelContext.GetMdpMessageTypes();
            _channelContext = channelContext;
            _requestForQuoteHandler = new RequestForQuoteHandler(channelContext);
            _securityStatusHandler = new SecurityStatusHandler(channelContext);
            _eventCommitFunction = securityId =>
            {
                var instController = _channelContext.FindInstrumentController(securityId, null);
                instController?.CommitEvent();
            };
        }

        public long GetPrcdSeqNum() => _prcdSeqNum;
        public ChannelState GetState() => _state;

        public void Lock() => System.Threading.Monitor.Enter(_lock);
        public void Unlock() => System.Threading.Monitor.Exit(_lock);

        public void SwitchState(ChannelState newState) => SwitchState(_state, newState);

        public void SwitchState(ChannelState prevState, ChannelState newState)
        {
            _state = newState;
            _channelContext.NotifyChannelStateListeners(prevState, newState);
        }

        public void HandleIncrementalPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            HandleIncrementalPacket(feedContext, mdpPacket, false);
        }

        public void HandleIncrementalPacket(MdpFeedContext feedContext, MdpPacket mdpPacket, bool fromQueue)
        {
            long msgSeqNum = mdpPacket.GetMsgSeqNum();
            lock (_lock)
            {
                _lastIncrPcktReceived = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                HandleIncrementalMessages(feedContext, msgSeqNum, mdpPacket);
            }
        }

        private void UpdateSecurityFromIncrementalRefresh(MdpFeedContext feedContext, long msgSeqNum,
            short matchEventIndicator, IMdpGroup incrGroup, InstrumentController? instController, int secId)
        {
            if (instController != null)
            {
                if (_channelContext.HasMdListeners())
                    _eventController.LogSecurity(secId);
                var entry = feedContext.GetMdpGroupEntryObj();
                incrGroup.GetEntry(entry);
                instController.OnIncrementalRefresh(feedContext, msgSeqNum, matchEventIndicator, entry);
            }
        }

        private void HandleMarketDataIncrementalRefresh(MdpFeedContext feedContext, IMdpMessage mdpMessage,
            long msgSeqNum, short matchEventIndicator)
        {
            var incrGroup = feedContext.GetMdpGroupObj();
            InstrumentController? instController = null;
            mdpMessage.GetGroup(NoMdEntries, incrGroup);
            while (incrGroup.HasNext())
            {
                incrGroup.Next();
                var mdEntryType = MDEntryTypeExtensions.FromFIX(incrGroup.GetChar(IncrRfrshMdEntryType));
                if (mdEntryType == MDEntryType.EmptyBook)
                {
                    HandleChannelReset(mdpMessage);
                }
                else
                {
                    int secId = incrGroup.GetInt32(SecurityId);
                    if (instController == null || instController.GetSecurityId() != secId)
                        instController = _channelContext.FindInstrumentController(secId, null);
                    UpdateSecurityFromIncrementalRefresh(feedContext, msgSeqNum, matchEventIndicator, incrGroup, instController, secId);
                }
            }
        }

        private void HandleChannelReset(IMdpMessage resetMessage)
        {
            _channelContext.NotifyChannelResetListeners(resetMessage);
            _prcdSeqNum = 0;
            _lastMsgSeqNumPrcd369 = 0;
            _wasChannelResetInPrcdPacket = true;
            _channelContext.GetInstruments().ResetAll();
            SwitchState(ChannelState.SYNC);
            if (_channelContext.HasMdListeners()) _eventController.Reset();
            _channelContext.NotifyChannelResetFinishedListeners(resetMessage);
        }

        private void HandleIncrementalMessages(MdpFeedContext feedContext, long msgSeqNum, MdpPacket mdpPacket)
        {
            foreach (var mdpMessage in mdpPacket)
            {
                var messageType = _mdpMessageTypes.GetMessageType(mdpMessage.GetSchemaId())!;
                mdpMessage.SetMessageType(messageType);

                if (messageType.GetSemanticMsgType() == SemanticMsgType.MarketDataIncrementalRefresh)
                {
                    short matchEventIndicator = mdpMessage.GetUInt8(SbeConstants.MATCHEVENTINDICATOR_TAG);
                    HandleMarketDataIncrementalRefresh(feedContext, mdpMessage, msgSeqNum, matchEventIndicator);
                    if (_channelContext.HasMdListeners() && MatchEventIndicator.HasEndOfEvent(matchEventIndicator))
                        _eventController.Commit(_eventCommitFunction);
                }
                else if (messageType.GetSemanticMsgType() == SemanticMsgType.QuoteRequest)
                {
                    _requestForQuoteHandler.Handle(feedContext, mdpMessage);
                }
                else if (messageType.GetSemanticMsgType() == SemanticMsgType.SecurityStatus)
                {
                    short matchEventIndicator = mdpMessage.GetUInt8(SbeConstants.MATCHEVENTINDICATOR_TAG);
                    _securityStatusHandler.Handle(mdpMessage, matchEventIndicator);
                    if (_channelContext.HasMdListeners() && MatchEventIndicator.HasEndOfEvent(matchEventIndicator))
                        _eventController.Commit(_eventCommitFunction);
                }
                else if (messageType.GetSemanticMsgType() == SemanticMsgType.SecurityDefinition)
                {
                    _channelContext.GetInstruments().OnMessage(feedContext, mdpMessage);
                }
            }

            if (_wasChannelResetInPrcdPacket)
                _wasChannelResetInPrcdPacket = false;
            else
                _prcdSeqNum = msgSeqNum;
        }

        private void StopSnapshotListening(MdpFeedContext feedContext)
        {
            _channelContext.StopSnapshotFeeds();
            if (_state != ChannelState.SYNC)
            {
                _prcdSeqNum = _lastMsgSeqNumPrcd369;
                if (_channelContext.HasMdListeners()) _eventController.Reset();
                if (_state == ChannelState.INITIAL) SwitchState(_state, ChannelState.SYNC);
            }
        }

        private void HandleSnapshotMessageInternal(MdpFeedContext feedContext, long snptPktSeqNum, IMdpMessage mdpMessage)
        {
            long lastMsgSeqNumProcessed = mdpMessage.GetUInt32(LastMsgSeqNumProcessed);
            if (snptPktSeqNum == 1 && CanStopSnapshotListening(_snptMsgCountDown))
            {
                StopSnapshotListening(feedContext);
                return;
            }
            HandleSnapshotMessage(feedContext, mdpMessage);
            if (_snptMsgCountDown == PrcdSnptCountNull)
            {
                int totalNumReports = (int)mdpMessage.GetUInt32(911) * SnapshotCyclesMax;
                _snptMsgCountDown = totalNumReports;
            }
            _snptMsgCountDown--;
            _lastMsgSeqNumPrcd369 = lastMsgSeqNumProcessed;
        }

        public void HandleSnapshotPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            lock (_lock)
            {
                foreach (var mdpMessage in mdpPacket)
                {
                    var messageType = _mdpMessageTypes.GetMessageType(mdpMessage.GetSchemaId())!;
                    if (messageType.GetSemanticMsgType() == SemanticMsgType.MarketDataSnapshotFullRefresh)
                    {
                        mdpMessage.SetMessageType(messageType);
                        HandleSnapshotMessageInternal(feedContext, mdpPacket.GetMsgSeqNum(), mdpMessage);
                    }
                }
            }
        }

        private void HandleSnapshotMessage(MdpFeedContext feedContext, IMdpMessage mdpMessage)
        {
            int secId = mdpMessage.GetInt32(48);
            var instController = _channelContext.FindInstrumentController(secId, null);
            instController?.OnSnapshotFullRefresh(feedContext, mdpMessage);
        }

        public void AddOutOfSyncInstrument(int instrumentId) => _outOfSyncInstruments.Add(instrumentId);
        public bool RemoveOutOfSyncInstrument(int instrumentId) => _outOfSyncInstruments.Remove(instrumentId);
        public bool HasOutOfSyncInstruments() => _outOfSyncInstruments.Count > 0;

        private bool CanStopSnapshotListening(int msgLeft) => !HasOutOfSyncInstruments() || msgLeft <= 0;

        public void ResetSnapshotCycleCount() => _snptMsgCountDown = PrcdSnptCountNull;

        public long GetLastIncrPcktReceived() => _lastIncrPcktReceived;

        public void Close() { }
    }
}
