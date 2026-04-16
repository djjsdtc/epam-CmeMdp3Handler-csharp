namespace Epam.CmeMdp3Handler.MktData
{
    public interface ISecurityStatistics
    {
        int GetSecurityId();
        double? FixingPrice();
        double? IndicativeOpeningPrice();
        double? OpeningPrice();
        int OpenInterest();
        double? SessionHighBid();
        double? SessionLowOffer();
        double? SettlementPrice();
        int TradeVolume();
        double? TradingSessionHighPrice();
        double? TradingSessionLowPrice();
        double? HighLimitPrice();
        double? LowLimitPrice();
        double? MaxPriceVariation();
    }
}
