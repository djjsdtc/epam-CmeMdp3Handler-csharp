using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation is not complete
    public class StatisticsHandler : AbstractMktDataHandler
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
                incrementEntry.GetDouble(270, _openingPrice);
            else if (openCloseSettlFlag == FlagIndicativeOpeningPrice)
                incrementEntry.GetDouble(270, _indicativeOpeningPrice);
        }

        public void UpdateSettlementPrice(IFieldSet incrementEntry)
        {
            throw new System.NotSupportedException();
        }

        public void UpdateTradingSessionHighPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionHighPrice);
        }

        public void UpdateTradingSessionLowPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowPrice);
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
            incrementEntry.GetDouble(270, _sessionHighBid);
        }

        public void UpdateSessionLowOffer(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowOffer);
        }

        public void UpdateFixingPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _fixingPrice);
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
