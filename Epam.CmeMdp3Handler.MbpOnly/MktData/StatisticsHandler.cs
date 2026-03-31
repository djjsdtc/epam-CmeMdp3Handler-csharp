using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation is not complete
    public class StatisticsHandler : AbstractMktDataHandler, ISecurityStatistics
    {
        private const short FlagDailyOpenPrice = 0;
        private const short FlagIndicativeOpeningPrice = 5;

        private readonly Price _openingPrice = new Price();
        private readonly Price _fixingPrice = new Price();
        private readonly Price _indicativeOpeningPrice = new Price();
        private readonly Price _sessionHighBid = new Price();
        private readonly Price _sessionLowOffer = new Price();
        private readonly Price _sessionHighPrice = new Price();
        private readonly Price _sessionLowPrice = new Price();
        private readonly Price _settlementPrice = new Price();
        private int _openInterest = 0;
        private int _clearedVolume = 0;

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
                _openingPrice.SetMantissa(incrementEntry.GetInt64(270));
            else if (openCloseSettlFlag == FlagIndicativeOpeningPrice)
                _indicativeOpeningPrice.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateSettlementPrice(IFieldSet incrementEntry)
        {
            _settlementPrice.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateTradingSessionHighPrice(IFieldSet incrementEntry)
        {
            _sessionHighPrice.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateTradingSessionLowPrice(IFieldSet incrementEntry)
        {
            _sessionLowPrice.SetMantissa(incrementEntry.GetInt64(270));
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
            _sessionHighBid.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateSessionLowOffer(IFieldSet incrementEntry)
        {
            _sessionLowOffer.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateFixingPrice(IFieldSet incrementEntry)
        {
            _fixingPrice.SetMantissa(incrementEntry.GetInt64(270));
            refreshed = true;
        }

        public void UpdateThresholdLimitsAndPriceBandVariation(IFieldSet incrementEntry)
        {
            // Not implemented
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
            refreshed = false;
        }

        public void CommitEvent()
        {
            if (refreshed) channelContext.NotifySecurityStatistics(this);
            refreshed = false;
        }

        public Price OpeningPrice() => _openingPrice;

        public Price SettlementPrice() => _settlementPrice;

        public Price TradingSessionHighPrice() => _sessionHighPrice;

        public Price TradingSessionLowPrice() => _sessionLowPrice;

        public Price FixingPrice() => _fixingPrice;

        public Price IndicativeOpeningPrice() => _indicativeOpeningPrice;

        public Price SessionHighBid() => _sessionHighBid;

        public Price SessionLowOffer() => _sessionLowOffer;

        public int OpenInterest() => _openInterest;

        public int TradeVolume() => _clearedVolume;
    }
}
