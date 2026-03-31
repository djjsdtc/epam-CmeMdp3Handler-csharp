using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MktData;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.Control
{
    public class InstrumentMdHandler
    {
        private readonly ChannelContext _channelContext;
        private readonly int _securityId;
        private int _subscriptionFlags;
        private byte _maxDepth;
        private bool _enabled;

        private ImpliedBookHandler? _impliedBookHandler;
        private MultipleDepthBookHandler? _multipleDepthBookHandler;
        private TradeHandler? _tradeHandler;
        private StatisticsHandler? _statisticsHandler;

        public InstrumentMdHandler(ChannelContext channelContext, int securityId, int subscriptionFlags, byte maxDepth)
        {
            _channelContext = channelContext;
            _securityId = securityId;
            _maxDepth = maxDepth;
            SetSubscriptionFlags(subscriptionFlags);
        }

        public int GetSubscriptionFlags() => _subscriptionFlags;
        public byte GetMaxDepth() => _maxDepth;

        public void SetSubscriptionFlags(int subscriptionFlags)
        {
            _enabled = false;
            _subscriptionFlags = subscriptionFlags;

            if (MdEventFlags.HasImpliedBook(subscriptionFlags) || MdEventFlags.HasImpliedTop(subscriptionFlags))
            {
                if (_impliedBookHandler == null)
                    _impliedBookHandler = new ImpliedBookHandler(_channelContext, _securityId, subscriptionFlags);
                else
                {
                    _impliedBookHandler.Clear();
                    _impliedBookHandler.SetSubscriptionFlags(subscriptionFlags);
                }
                _enabled = true;
            }
            else
            {
                _impliedBookHandler = null;
            }

            if (MdEventFlags.HasBook(subscriptionFlags) || MdEventFlags.HasTop(subscriptionFlags))
            {
                if (_multipleDepthBookHandler == null)
                    _multipleDepthBookHandler = new MultipleDepthBookHandler(_channelContext, _securityId, subscriptionFlags, _maxDepth);
                else
                {
                    _multipleDepthBookHandler.Clear();
                    _multipleDepthBookHandler.SetSubscriptionFlags(subscriptionFlags);
                }
                _enabled = true;
            }
            else
            {
                _multipleDepthBookHandler = null;
            }

            if (MdEventFlags.HasTradeSummary(subscriptionFlags))
            {
                if (_tradeHandler == null)
                    _tradeHandler = new TradeHandler(_channelContext, _securityId, subscriptionFlags);
                else
                {
                    _tradeHandler.Clear();
                    _tradeHandler.SetSubscriptionFlags(subscriptionFlags);
                }
                _enabled = true;
            }
            else
            {
                _tradeHandler = null;
            }

            if (MdEventFlags.HasStatistics(subscriptionFlags))
            {
                if (_statisticsHandler == null)
                    _statisticsHandler = new StatisticsHandler(_channelContext, _securityId, _subscriptionFlags);
                else
                {
                    _statisticsHandler.Clear();
                    _statisticsHandler.SetSubscriptionFlags(subscriptionFlags);
                }
                _enabled = true;
            }
            else
            {
                _statisticsHandler = null;
            }
        }

        public void AddSubscriptionFlag(int subscriptionFlags)
        {
            _subscriptionFlags |= subscriptionFlags;
            SetSubscriptionFlags(_subscriptionFlags);
        }

        public void RemoveSubscriptionFlag(int subscriptionFlags)
        {
            _subscriptionFlags ^= subscriptionFlags;
            SetSubscriptionFlags(_subscriptionFlags);
        }

        public void Reset()
        {
            _impliedBookHandler?.Clear();
            _multipleDepthBookHandler?.Clear();
            _tradeHandler?.Clear();
            _multipleDepthBookHandler?.Clear();
        }

        public void HandleSnapshotFullRefreshEntries(MdpFeedContext feedContext, IMdpMessage fullRefreshMsg)
        {
            if (!_enabled) return;

            var mdpGroupObj = feedContext.GetMdpGroupObj();
            fullRefreshMsg.GetGroup(268, mdpGroupObj);
            while (mdpGroupObj.HasNext())
            {
                mdpGroupObj.Next();
                var mdEntryType = MDEntryTypeExtensions.FromFIX(mdpGroupObj.GetChar(269));
                switch (mdEntryType)
                {
                    case MDEntryType.Bid:
                        _multipleDepthBookHandler?.HandleSnapshotBidEntry(mdpGroupObj);
                        break;
                    case MDEntryType.Offer:
                        _multipleDepthBookHandler?.HandleSnapshotOfferEntry(mdpGroupObj);
                        break;
                    case MDEntryType.Trade:
                        _tradeHandler?.UpdateTradeSummary(mdpGroupObj);
                        break;
                    case MDEntryType.OpeningPrice:
                        _statisticsHandler?.UpdateOpeningPrice(mdpGroupObj);
                        break;
                    case MDEntryType.SettlementPrice:
                        _statisticsHandler?.UpdateSettlementPrice(mdpGroupObj);
                        break;
                    case MDEntryType.TradingSessionHighPrice:
                        _statisticsHandler?.UpdateTradingSessionHighPrice(mdpGroupObj);
                        break;
                    case MDEntryType.TradingSessionLowPrice:
                        _statisticsHandler?.UpdateTradingSessionLowPrice(mdpGroupObj);
                        break;
                    case MDEntryType.TradeVolume:
                        _statisticsHandler?.UpdateTradeVolume(mdpGroupObj);
                        break;
                    case MDEntryType.OpenInterest:
                        _statisticsHandler?.UpdateOpenInterest(mdpGroupObj);
                        break;
                    case MDEntryType.ImpliedBid:
                        _impliedBookHandler?.HandleSnapshotBidEntry(mdpGroupObj);
                        break;
                    case MDEntryType.ImpliedOffer:
                        _impliedBookHandler?.HandleSnapshotOfferEntry(mdpGroupObj);
                        break;
                    case MDEntryType.SessionHighBid:
                        _statisticsHandler?.UpdateSessionHighBid(mdpGroupObj);
                        break;
                    case MDEntryType.SessionLowOffer:
                        _statisticsHandler?.UpdateSessionLowOffer(mdpGroupObj);
                        break;
                    case MDEntryType.FixingPrice:
                        _statisticsHandler?.UpdateFixingPrice(mdpGroupObj);
                        break;
                    case MDEntryType.ElectronicVolume:
                        _tradeHandler?.UpdateElectronicVolume(mdpGroupObj);
                        break;
                    case MDEntryType.ThresholdLimitsandPriceBandVariation:
                        _statisticsHandler?.UpdateThresholdLimitsAndPriceBandVariation(mdpGroupObj);
                        break;
                    default:
                        throw new System.InvalidOperationException($"Unexpected MDEntryType in snapshot: {mdEntryType}");
                }
            }

            if (_multipleDepthBookHandler != null)
                _channelContext.NotifyBookFullRefresh(_multipleDepthBookHandler);
            if (_impliedBookHandler != null)
                _channelContext.NotifyImpliedBookFullRefresh(_impliedBookHandler);
            if (_statisticsHandler != null)
                _channelContext.NotifySecurityStatistics(_statisticsHandler);
        }

        public void HandleIncrementalRefreshEntry(IFieldSet incrementEntry)
        {
            if (!_enabled) return;

            var mdEntryType = MDEntryTypeExtensions.FromFIX(incrementEntry.GetChar(269));
            switch (mdEntryType)
            {
                case MDEntryType.Bid:
                    _multipleDepthBookHandler?.HandleIncrementBidEntry(incrementEntry);
                    break;
                case MDEntryType.Offer:
                    _multipleDepthBookHandler?.HandleIncrementOfferEntry(incrementEntry);
                    break;
                case MDEntryType.Trade:
                    _tradeHandler?.UpdateTradeSummary(incrementEntry);
                    break;
                case MDEntryType.OpeningPrice:
                    _statisticsHandler?.UpdateOpeningPrice(incrementEntry);
                    break;
                case MDEntryType.SettlementPrice:
                    _statisticsHandler?.UpdateSettlementPrice(incrementEntry);
                    break;
                case MDEntryType.TradingSessionHighPrice:
                    _statisticsHandler?.UpdateTradingSessionHighPrice(incrementEntry);
                    break;
                case MDEntryType.TradingSessionLowPrice:
                    _statisticsHandler?.UpdateTradingSessionLowPrice(incrementEntry);
                    break;
                case MDEntryType.TradeVolume:
                    _statisticsHandler?.UpdateTradeVolume(incrementEntry);
                    break;
                case MDEntryType.OpenInterest:
                    _statisticsHandler?.UpdateOpenInterest(incrementEntry);
                    break;
                case MDEntryType.ImpliedBid:
                    _impliedBookHandler?.HandleIncrementBidEntry(incrementEntry);
                    break;
                case MDEntryType.ImpliedOffer:
                    _impliedBookHandler?.HandleIncrementOfferEntry(incrementEntry);
                    break;
                case MDEntryType.EmptyBook:
                    ClearBooks();
                    break;
                case MDEntryType.SessionHighBid:
                    _statisticsHandler?.UpdateSessionHighBid(incrementEntry);
                    break;
                case MDEntryType.SessionLowOffer:
                    _statisticsHandler?.UpdateSessionLowOffer(incrementEntry);
                    break;
                case MDEntryType.FixingPrice:
                    _statisticsHandler?.UpdateFixingPrice(incrementEntry);
                    break;
                case MDEntryType.ElectronicVolume:
                    _tradeHandler?.UpdateElectronicVolume(incrementEntry);
                    break;
                case MDEntryType.ThresholdLimitsandPriceBandVariation:
                    _statisticsHandler?.UpdateThresholdLimitsAndPriceBandVariation(incrementEntry);
                    break;
                default:
                    throw new System.InvalidOperationException($"Unexpected MDEntryType in incremental: {mdEntryType}");
            }
        }

        private void ClearBooks()
        {
            _multipleDepthBookHandler?.Clear();
            _impliedBookHandler?.Clear();
            _statisticsHandler?.Clear();
        }

        internal void CommitEvent()
        {
            if (!_enabled) return;
            _multipleDepthBookHandler?.CommitEvent();
            _impliedBookHandler?.CommitEvent();
            _statisticsHandler?.CommitEvent();
        }
    }
}
