using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;
using Microsoft.Extensions.Logging;
using static Epam.CmeMdp3Handler.Control.IncrementalRefreshQueue;
using static Epam.CmeMdp3Handler.MktData.MdConstants;

namespace Epam.CmeMdp3Handler.Control
{
    public class InstrumentController
    {
        public const int DefQueueSlotInitBufferSize = 50;
        public const int DefIncrQueueSize = 1000;
        public const int DefGapThreshold = 5;

        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<InstrumentController>();

        private readonly int _securityId;
        private string? _secDesc;
        private readonly ChannelContext _channelContext;
        private InstrumentState _state = InstrumentState.INITIAL;
        private long _processedRptSeqNum = 0;
        private readonly InstrumentMdHandler _mdHandler;
        private int _gapThreshold = DefGapThreshold;
        private readonly IncrementalRefreshQueue _incrRefreshQueue;
        private readonly IMdpGroupEntry _incrGroupEntry = SbeGroupEntry.Instance();
        private readonly IncrementalRefreshQueueEntry _incrQueueEntry;

        public InstrumentController(ChannelContext channelContext, int securityId, string? secDesc,
            int subscriptionFlags, byte maxDepth, int gapThreshold)
        {
            _channelContext = channelContext;
            _securityId = securityId;
            _secDesc = secDesc;
            _gapThreshold = gapThreshold;
            _mdHandler = new InstrumentMdHandler(channelContext, securityId, subscriptionFlags, maxDepth);
            _incrRefreshQueue = new IncrementalRefreshQueue(channelContext.GetIncrQueueSize(),
                channelContext.GetQueueSlotInitBufferSize());
            _incrQueueEntry = new IncrementalRefreshQueueEntry(_incrGroupEntry);
            Init(subscriptionFlags);
        }

        private void Init(int subscriptionFlags)
        {
            _mdHandler.SetSubscriptionFlags(subscriptionFlags);
            _channelContext.SubscribeToSnapshotsForInstrument(_securityId);
            _channelContext.NotifyInstrumentStateListeners(_securityId, _secDesc, InstrumentState.NEW, InstrumentState.INITIAL);
        }

        public int GetSubscriptionFlags() => _mdHandler.GetSubscriptionFlags();
        public void SetSubscriptionFlags(int subscrFlags) => _mdHandler.SetSubscriptionFlags(subscrFlags);
        public void AddSubscriptionFlag(int subscriptionFlags) => _mdHandler.AddSubscriptionFlag(subscriptionFlags);
        public void RemoveSubscriptionFlag(int subscriptionFlags) => _mdHandler.RemoveSubscriptionFlag(subscriptionFlags);
        public void SetSecDesc(string secDesc) => _secDesc = secDesc;

        public void Discontinue()
        {
            SwitchState(_state, InstrumentState.DISCONTINUED);
            _mdHandler.SetSubscriptionFlags(MdEventFlags.NOTHING);
            _processedRptSeqNum = 0;
            _mdHandler.Reset();
        }

        public bool OnResubscribe(int subscriptionFlags)
        {
            if (_state == InstrumentState.DISCONTINUED)
            {
                SetSubscriptionFlags(subscriptionFlags);
                SwitchState(InstrumentState.DISCONTINUED, InstrumentState.INITIAL);
                return true;
            }
            return false;
        }

        public void OnChannelReset()
        {
            _processedRptSeqNum = 0;
            _incrRefreshQueue.Clear();
            if (_state != InstrumentState.DISCONTINUED)
                SwitchState(_state, InstrumentState.SYNC);
            _mdHandler.Reset();
        }

        private void HandleSnapshotFullRefreshEntries(MdpFeedContext feedContext, IMdpMessage fullRefreshMsg)
        {
            _channelContext.NotifySnapshotFullRefreshListeners(_secDesc, fullRefreshMsg);
            _mdHandler.HandleSnapshotFullRefreshEntries(feedContext, fullRefreshMsg);
        }

        private bool HandleSecurityRefreshInQueue(IncrementalRefreshQueueEntry incrQueueEntry)
        {
            long rptSeqNum = incrQueueEntry.GroupEntry.GetUInt32(RptSeqNum);
            long expectedRptSeqNum = _processedRptSeqNum + 1;
            short matchEventIndicator = incrQueueEntry.MatchEventIndicator;
            var incrGroupEntry = incrQueueEntry.GroupEntry;

            if (rptSeqNum == expectedRptSeqNum)
            {
                _processedRptSeqNum = rptSeqNum;
                _channelContext.NotifyIncrementalRefreshListeners(matchEventIndicator, _securityId, _secDesc,
                    incrQueueEntry.IncrPcktSeqNum, incrGroupEntry);
                _mdHandler.HandleIncrementalRefreshEntry(incrQueueEntry.GroupEntry);
            }
            else if (rptSeqNum > (expectedRptSeqNum + _gapThreshold))
            {
                SwitchState(InstrumentState.SYNC, InstrumentState.OUTOFSYNC);
                return false;
            }
            return true;
        }

        private void HandleIncrementalQueue(MdpFeedContext feedContext, long prcdSeqNum)
        {
            var queue = _incrRefreshQueue;
            for (long i = prcdSeqNum + 1; i <= queue.GetLastRptSeqNum(); i++)
            {
                if (queue.Poll(i, _incrQueueEntry) > 0)
                {
                    if (!HandleSecurityRefreshInQueue(_incrQueueEntry))
                        return;
                }
                else
                {
                    return;
                }
            }
        }

        private void SwitchState(InstrumentState prevState, InstrumentState newState)
        {
            _state = newState;
            if (newState == InstrumentState.OUTOFSYNC || newState == InstrumentState.INITIAL)
                _channelContext.SubscribeToSnapshotsForInstrument(_securityId);
            else if (newState == InstrumentState.SYNC || newState == InstrumentState.DISCONTINUED)
                _channelContext.UnsubscribeToSnapshotsForInstrument(_securityId);
            _channelContext.NotifyInstrumentStateListeners(_securityId, _secDesc, prevState, newState);
        }

        private void HandleIncrementalRefreshEntry(long msgSeqNum, short matchEventIndicator, IFieldSet incrRefreshEntry)
        {
            _channelContext.NotifyIncrementalRefreshListeners(matchEventIndicator, _securityId, _secDesc, msgSeqNum, incrRefreshEntry);
            _mdHandler.HandleIncrementalRefreshEntry(incrRefreshEntry);
        }

        private void PushIncrementalRefreshEntryInQueue(long msgSeqNum, short matchEventIndicator,
            long rptSeqNum, IMdpGroupEntry incrRefreshEntry)
        {
            _incrQueueEntry.IncrPcktSeqNum = msgSeqNum;
            _incrQueueEntry.MatchEventIndicator = matchEventIndicator;
            _incrQueueEntry.GroupEntry = incrRefreshEntry;
            _incrRefreshQueue.Push(rptSeqNum, _incrQueueEntry);
        }

        internal void CommitEvent() => _mdHandler.CommitEvent();

        public int GetSecurityId() => _securityId;

        internal void OnSnapshotFullRefresh(MdpFeedContext feedContext, IMdpMessage fullRefreshMsg)
        {
            if (fullRefreshMsg.HasField(RptSeqNum))
            {
                var currentState = _state;
                long snptSeqNum = fullRefreshMsg.GetUInt32(RptSeqNum);

                if (currentState == InstrumentState.INITIAL)
                {
                    _processedRptSeqNum = snptSeqNum;
                    SwitchState(currentState, InstrumentState.SYNC);
                    HandleSnapshotFullRefreshEntries(feedContext, fullRefreshMsg);
                    HandleIncrementalQueue(feedContext, snptSeqNum);
                }
                else if (currentState == InstrumentState.OUTOFSYNC)
                {
                    if (snptSeqNum > _processedRptSeqNum)
                    {
                        _processedRptSeqNum = snptSeqNum;
                        _mdHandler.Reset();
                        SwitchState(currentState, InstrumentState.SYNC);
                        HandleSnapshotFullRefreshEntries(feedContext, fullRefreshMsg);
                        HandleIncrementalQueue(feedContext, snptSeqNum);
                    }
                }
                else if (currentState == InstrumentState.SYNC && snptSeqNum > _processedRptSeqNum)
                {
                    _processedRptSeqNum = snptSeqNum;
                    _mdHandler.Reset();
                    HandleSnapshotFullRefreshEntries(feedContext, fullRefreshMsg);
                    HandleIncrementalQueue(feedContext, snptSeqNum);
                }
            }
        }

        internal void OnIncrementalRefresh(MdpFeedContext feedContext, long msgSeqNum,
            short matchEventIndicator, IMdpGroupEntry incrRefreshEntry)
        {
            var currentState = _state;
            if (incrRefreshEntry.HasField(RptSeqNum))
            {
                long rptSeqNum = incrRefreshEntry.GetUInt32(RptSeqNum);
                long expectedRptSeqNum = _processedRptSeqNum + 1;

                if (currentState == InstrumentState.SYNC)
                {
                    if (rptSeqNum == expectedRptSeqNum)
                    {
                        _processedRptSeqNum = rptSeqNum;
                        HandleIncrementalRefreshEntry(msgSeqNum, matchEventIndicator, incrRefreshEntry);
                        HandleIncrementalQueue(feedContext, rptSeqNum);
                    }
                    else if (rptSeqNum > expectedRptSeqNum)
                    {
                        PushIncrementalRefreshEntryInQueue(msgSeqNum, matchEventIndicator, rptSeqNum, incrRefreshEntry);
                        if (rptSeqNum > (expectedRptSeqNum + _gapThreshold))
                        {
                            _mdHandler.Reset();
                            SwitchState(InstrumentState.SYNC, InstrumentState.OUTOFSYNC);
                        }
                    }
                }
                else if (currentState == InstrumentState.OUTOFSYNC)
                {
                    if (rptSeqNum == expectedRptSeqNum)
                    {
                        _processedRptSeqNum = rptSeqNum;
                        SwitchState(currentState, InstrumentState.SYNC);
                        HandleIncrementalRefreshEntry(msgSeqNum, matchEventIndicator, incrRefreshEntry);
                        HandleIncrementalQueue(feedContext, rptSeqNum);
                    }
                    else if (rptSeqNum > expectedRptSeqNum)
                    {
                        PushIncrementalRefreshEntryInQueue(msgSeqNum, matchEventIndicator, rptSeqNum, incrRefreshEntry);
                    }
                }
                else if (currentState == InstrumentState.INITIAL)
                {
                    if (_processedRptSeqNum == 0 && rptSeqNum == 1)
                    {
                        _processedRptSeqNum = rptSeqNum;
                        SwitchState(currentState, InstrumentState.SYNC);
                        HandleIncrementalRefreshEntry(msgSeqNum, matchEventIndicator, incrRefreshEntry);
                    }
                    else
                    {
                        PushIncrementalRefreshEntryInQueue(msgSeqNum, matchEventIndicator, rptSeqNum, incrRefreshEntry);
                    }
                }
            }
        }
    }
}
