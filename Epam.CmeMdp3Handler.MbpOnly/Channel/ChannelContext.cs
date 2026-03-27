using System.Collections.Generic;
using Epam.CmeMdp3Handler.Control;
using Epam.CmeMdp3Handler.MktData;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Schema;
using static Epam.CmeMdp3Handler.MktData.MdConstants;

namespace Epam.CmeMdp3Handler.Channel
{
    public class ChannelContext
    {
        private readonly MdpChannelImpl _channel;
        private readonly MdpMessageTypes _mdpMessageTypes;
        private int _gapThreshold;

        public ChannelContext(MdpChannelImpl channel, MdpMessageTypes mdpMessageTypes, int gapThreshold)
        {
            _channel = channel;
            _mdpMessageTypes = mdpMessageTypes;
            _gapThreshold = gapThreshold;
        }

        public MdpChannelImpl GetChannel() => _channel;

        public InstrumentController? FindInstrumentController(int securityId, string? secDesc)
            => _channel.FindController(securityId, secDesc);

        public ChannelInstruments GetInstruments() => _channel.Instruments;

        public MdpMessageTypes GetMdpMessageTypes() => _mdpMessageTypes;

        public bool HasMdListeners() => _channel.HasMdListener();

        public bool IsSnapshotFeedsActive() => _channel.IsSnapshotFeedsActive();

        public int GetGapThreshold() => _gapThreshold;
        public void SetGapThreshold(int gapThreshold) => _gapThreshold = gapThreshold;

        public int NotifySecurityDefinitionListeners(IMdpMessage mdpMessage)
        {
            int flags = MdEventFlags.NOTHING;
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                flags |= listeners[i].OnSecurityDefinition(_channel.GetId(), mdpMessage);
            return flags;
        }

        public void NotifyFeedStartedListeners(FeedType feedType, Feed feed)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnFeedStarted(_channel.GetId(), feedType, feed);
        }

        public void NotifyFeedStoppedListeners(FeedType feedType, Feed feed)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnFeedStopped(_channel.GetId(), feedType, feed);
        }

        public void NotifyImpliedBookRefresh(IImpliedBook impliedBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnImpliedBookRefresh(_channel.GetId(), impliedBook.GetSecurityId(), impliedBook);
        }

        public void NotifyImpliedBookFullRefresh(IImpliedBook impliedBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnImpliedBookFullRefresh(_channel.GetId(), impliedBook.GetSecurityId(), impliedBook);
        }

        public void NotifyImpliedTopOfBookRefresh(IImpliedBook impliedBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnTopOfImpliedBookRefresh(_channel.GetId(), impliedBook.GetSecurityId(),
                    impliedBook.GetBid(TopOfTheBookLevel),
                    impliedBook.GetOffer(TopOfTheBookLevel));
        }

        public void NotifyBookRefresh(IOrderBook orderBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnOrderBookRefresh(_channel.GetId(), orderBook.GetSecurityId(), orderBook);
        }

        public void NotifyBookFullRefresh(IOrderBook orderBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnOrderBookFullRefresh(_channel.GetId(), orderBook.GetSecurityId(), orderBook);
        }

        public void NotifyTopOfBookRefresh(IOrderBook orderBook)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnTopOfBookRefresh(_channel.GetId(), orderBook.GetSecurityId(),
                    orderBook.GetBid(TopOfTheBookLevel),
                    orderBook.GetOffer(TopOfTheBookLevel));
        }

        public void NotifyIncrementalRefreshListeners(short matchEventIndicator, int securityId,
            string? secDesc, long msgSeqNum, IFieldSet mdpGroupEntry)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnIncrementalRefresh(_channel.GetId(), matchEventIndicator, securityId,
                    secDesc, msgSeqNum, mdpGroupEntry);
        }

        public void NotifySnapshotFullRefreshListeners(string? secDesc, IMdpMessage mdpMessage)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnSnapshotFullRefresh(_channel.GetId(), secDesc, mdpMessage);
        }

        public void NotifyChannelResetListeners(IMdpMessage mdpMessage)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnBeforeChannelReset(_channel.GetId(), mdpMessage);
        }

        public void NotifyChannelResetFinishedListeners(IMdpMessage mdpMessage)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnFinishedChannelReset(_channel.GetId(), mdpMessage);
        }

        public void NotifyChannelStateListeners(ChannelState prevState, ChannelState newState)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnChannelStateChanged(_channel.GetId(), prevState, newState);
        }

        public void NotifyInstrumentStateListeners(int securityId, string? secDesc,
            InstrumentState prevState, InstrumentState newState)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnInstrumentStateChanged(_channel.GetId(), securityId, secDesc, prevState, newState);
        }

        public void NotifyRequestForQuote(SbeString quoReqId, int entryIdx, int entryNum,
            int securityId, QuoteType quoteType, int orderQty, Side? side)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnRequestForQuote(_channel.GetId(), quoReqId, entryIdx, entryNum,
                    securityId, quoteType, orderQty, side);
        }

        public void NotifySecurityStatus(int securityId, IMdpMessage secStatusMessage)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnSecurityStatus(_channel.GetId(), securityId, secStatusMessage);
        }

        public void NotifyPacketReceived(FeedType feedType, Feed feed, MdpPacket mdpPacket)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnPacket(_channel.GetId(), feedType, feed, mdpPacket);
        }

        public void NotifyRequestForQuote(IMdpMessage rfqMessage)
        {
            var listeners = _channel.GetListeners();
            for (int i = 0; i < listeners.Count; i++)
                listeners[i].OnRequestForQuote(_channel.GetId(), rfqMessage);
        }

        public void NotifySecurityStatus(SbeString secGroup, SbeString secAsset, int securityId,
            int tradeDate, short matchEventIndicator, SecurityTradingStatus? secTrdStatus,
            HaltReason? haltRsn, SecurityTradingEvent secTrdEvnt)
        {
            var mdListeners = _channel.GetMdListeners();
            for (int i = 0; i < mdListeners.Count; i++)
                mdListeners[i].OnSecurityStatus(_channel.GetId(), secGroup, secAsset, securityId,
                    tradeDate, matchEventIndicator, secTrdStatus, haltRsn, secTrdEvnt);
        }

        public void StopInstrumentFeeds()
        {
            _channel.StopInstrumentFeedA();
            _channel.StopInstrumentFeedB();
        }

        public void StopSnapshotFeeds() => _channel.StopSnapshotFeeds();
        public void StartSnapshotFeeds() => _channel.StartSnapshotFeeds();

        public void SubscribeToSnapshotsForInstrument(int securityId)
            => _channel.SubscribeToSnapshotsForInstrument(securityId);

        public void UnsubscribeToSnapshotsForInstrument(int securityId)
            => _channel.UnsubscribeFromSnapshotsForInstrument(securityId);

        public long GetPrcdSeqNum() => _channel.GetController().GetPrcdSeqNum();

        public ChannelState GetChannelState() => _channel.GetState();

        public int GetQueueSlotInitBufferSize() => _channel.GetQueueSlotInitBufferSize();
        public int GetIncrQueueSize() => _channel.GetIncrQueueSize();
    }
}
