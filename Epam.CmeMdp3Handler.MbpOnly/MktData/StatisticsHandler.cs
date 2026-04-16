using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation is not complete
    public class StatisticsHandler : AbstractMktDataHandler, ISecurityStatistics
    {
        private const short FlagDailyOpenPrice = 0;
        private const short FlagIndicativeOpeningPrice = 5;

        private readonly SbeDouble _openingPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _fixingPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _indicativeOpeningPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _sessionHighBid = SbeDouble.NullInstance();
        private readonly SbeDouble _sessionLowOffer = SbeDouble.NullInstance();
        private readonly SbeDouble _sessionHighPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _sessionLowPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _settlementPrice = SbeDouble.NullInstance();
        private int _openInterest = 0;
        private int _clearedVolume = 0;
        private readonly SbeDouble _highLimitPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _lowLimitPrice = SbeDouble.NullInstance();
        private readonly SbeDouble _maxPriceVariation = SbeDouble.NullInstance();

        private bool refreshed = false;

        public StatisticsHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
            this.subscriptionFlags = subscriptionFlags;
        }

        public override void SetSubscriptionFlags(int subscriptionFlags)
        {
            this.subscriptionFlags = subscriptionFlags;
        }

        public void UpdateOpeningPrice(IFieldSet incrementEntry)
        {
            short openCloseSettlFlag = incrementEntry.GetUInt8(286);
            if (openCloseSettlFlag == FlagDailyOpenPrice)
                incrementEntry.GetDouble(270, _openingPrice);
            else if (openCloseSettlFlag == FlagIndicativeOpeningPrice)
                incrementEntry.GetDouble(270, _indicativeOpeningPrice);
            refreshed = true;
        }

        public void UpdateSettlementPrice(IFieldSet incrementEntry)
        {
            _settlementPrice.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateTradingSessionHighPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionHighPrice);
            refreshed = true;
        }

        public void UpdateTradingSessionLowPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowPrice);
            refreshed = true;
        }

        public void UpdateTradeVolume(IFieldSet incrementEntry)
        {
            _clearedVolume = incrementEntry.GetInt32(271);
            refreshed = true;
        }

        public void UpdateOpenInterest(IFieldSet incrementEntry)
        {
            _openInterest = incrementEntry.GetInt32(271);
            refreshed = true;
        }

        public void UpdateSessionHighBid(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionHighBid);
            refreshed = true;
        }

        public void UpdateSessionLowOffer(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowOffer);
            refreshed = true;
        }

        public void UpdateFixingPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _fixingPrice);
            refreshed = true;
        }

        public void UpdateThresholdLimitsAndPriceBandVariation(IFieldSet incrementEntry)
        {
            _highLimitPrice.SetMantissa(incrementEntry.GetInt64(1149));
            _lowLimitPrice.SetMantissa(incrementEntry.GetInt64(1148));
            _maxPriceVariation.SetMantissa(incrementEntry.GetInt64(1143));
            refreshed = true;
        }

        public override void Clear()
        {
            _openingPrice.SetNull();
            _fixingPrice.SetNull();
            _indicativeOpeningPrice.SetNull();
            _sessionHighBid.SetNull();
            _sessionLowOffer.SetNull();
            _sessionHighPrice.SetNull();
            _sessionLowPrice.SetNull();
            _settlementPrice.SetNull();
            _openInterest = 0;
            _clearedVolume = 0;
            _highLimitPrice.SetNull();
            _lowLimitPrice.SetNull();
            _maxPriceVariation.SetNull();
            refreshed = false;
        }

        public void CommitEvent()
        {
            if (refreshed) channelContext.NotifySecurityStatistics(this);
            refreshed = false;
        }

        public double? OpeningPrice() => _openingPrice;

        public double? SettlementPrice() => _settlementPrice;

        public double? TradingSessionHighPrice() => _sessionHighPrice;

        public double? TradingSessionLowPrice() => _sessionLowPrice;

        public double? FixingPrice() => _fixingPrice;

        public double? IndicativeOpeningPrice() => _indicativeOpeningPrice;

        public double? SessionHighBid() => _sessionHighBid;

        public double? SessionLowOffer() => _sessionLowOffer;

        public int OpenInterest() => _openInterest;

        public int TradeVolume() => _clearedVolume;

        public double? HighLimitPrice() => _highLimitPrice;

        public double? LowLimitPrice() => _lowLimitPrice;

        public double? MaxPriceVariation() => _maxPriceVariation;
    }
}
