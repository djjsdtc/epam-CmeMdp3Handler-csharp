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

        private bool _refreshed = false;

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
            _refreshed = true;
        }

        public void UpdateSettlementPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _settlementPrice);
            _refreshed = true;
        }

        public void UpdateTradingSessionHighPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionHighPrice);
            _refreshed = true;
        }

        public void UpdateTradingSessionLowPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowPrice);
            _refreshed = true;
        }

        public void UpdateTradeVolume(IFieldSet incrementEntry)
        {
            _clearedVolume = incrementEntry.GetInt32(271);
            _refreshed = true;
        }

        public void UpdateOpenInterest(IFieldSet incrementEntry)
        {
            _openInterest = incrementEntry.GetInt32(271);
            _refreshed = true;
        }

        public void UpdateSessionHighBid(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionHighBid);
            _refreshed = true;
        }

        public void UpdateSessionLowOffer(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _sessionLowOffer);
            _refreshed = true;
        }

        public void UpdateFixingPrice(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(270, _fixingPrice);
            _refreshed = true;
        }

        public void UpdateThresholdLimitsAndPriceBandVariation(IFieldSet incrementEntry)
        {
            incrementEntry.GetDouble(1149, _highLimitPrice);
            incrementEntry.GetDouble(1148, _lowLimitPrice);
            incrementEntry.GetDouble(1143, _maxPriceVariation);
            _refreshed = true;
        }

        public override void Clear()
        {
            _openingPrice.Reset(true);
            _fixingPrice.Reset(true);
            _indicativeOpeningPrice.Reset(true);
            _sessionHighBid.Reset(true);
            _sessionLowOffer.Reset(true);
            _sessionHighPrice.Reset(true);
            _sessionLowPrice.Reset(true);
            _settlementPrice.Reset(true);
            _openInterest = 0;
            _clearedVolume = 0;
            _highLimitPrice.Reset(true);
            _lowLimitPrice.Reset(true);
            _maxPriceVariation.Reset(true);
            _refreshed = false;
        }

        public void CommitEvent()
        {
            if (_refreshed) channelContext.NotifySecurityStatistics(this);
            _refreshed = false;
        }

        public double? OpeningPrice() => _openingPrice.AsNullableDouble();

        public double? SettlementPrice() => _settlementPrice.AsNullableDouble();

        public double? TradingSessionHighPrice() => _sessionHighPrice.AsNullableDouble();

        public double? TradingSessionLowPrice() => _sessionLowPrice.AsNullableDouble();

        public double? FixingPrice() => _fixingPrice.AsNullableDouble();

        public double? IndicativeOpeningPrice() => _indicativeOpeningPrice.AsNullableDouble();

        public double? SessionHighBid() => _sessionHighBid.AsNullableDouble();

        public double? SessionLowOffer() => _sessionLowOffer.AsNullableDouble();

        public int OpenInterest() => _openInterest;

        public int TradeVolume() => _clearedVolume;

        public double? HighLimitPrice() => _highLimitPrice.AsNullableDouble();

        public double? LowLimitPrice() => _lowLimitPrice.AsNullableDouble();

        public double? MaxPriceVariation() => _maxPriceVariation.AsNullableDouble();

        public bool IsRefreshed() => _refreshed;
    }
}
