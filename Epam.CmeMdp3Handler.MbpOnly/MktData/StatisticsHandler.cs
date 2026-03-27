using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation is not complete
    public class StatisticsHandler : AbstractMktDataHandler
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
        private int _openInterest = 0;
        private int _clearedVolume = 0;

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
        }

        public void UpdateSettlementPrice(IFieldSet incrementEntry)
        {
            throw new System.NotSupportedException();
        }

        public void UpdateTradingSessionHighPrice(IFieldSet incrementEntry)
        {
            _sessionHighPrice.SetMantissa(incrementEntry.GetInt64(270));
        }

        public void UpdateTradingSessionLowPrice(IFieldSet incrementEntry)
        {
            _sessionLowPrice.SetMantissa(incrementEntry.GetInt64(270));
        }

        public void UpdateTradeVolume(IFieldSet incrementEntry)
        {
            _clearedVolume = incrementEntry.GetInt32(271);
        }

        public void UpdateOpenInterest(IFieldSet incrementEntry)
        {
            _openInterest = incrementEntry.GetInt32(271);
        }

        public void UpdateSessionHighBid(IFieldSet incrementEntry)
        {
            _sessionHighBid.SetMantissa(incrementEntry.GetInt64(270));
        }

        public void UpdateSessionLowOffer(IFieldSet incrementEntry)
        {
            _sessionLowOffer.SetMantissa(incrementEntry.GetInt64(270));
        }

        public void UpdateFixingPrice(IFieldSet incrementEntry)
        {
            _fixingPrice.SetMantissa(incrementEntry.GetInt64(270));
        }

        public void UpdateThresholdLimitsAndPriceBandVariation(IFieldSet incrementEntry)
        {
            throw new System.NotSupportedException();
        }

        public override void Clear()
        {
            throw new System.NotSupportedException();
        }
    }
}
