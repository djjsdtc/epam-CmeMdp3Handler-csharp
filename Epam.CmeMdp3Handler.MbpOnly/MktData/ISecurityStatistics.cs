namespace Epam.CmeMdp3Handler.MktData
{
    public interface ISecurityStatistics
    {
        int GetSecurityId();
        Price FixingPrice();
        Price IndicativeOpeningPrice();
        Price OpeningPrice();
        int OpenInterest();
        Price SessionHighBid();
        Price SessionLowOffer();
        Price SettlementPrice();
        int TradeVolume();
        Price TradingSessionHighPrice();
        Price TradingSessionLowPrice();
        Price HighLimitPrice();
        Price LowLimitPrice();
        Price MaxPriceVariation();
    }
}
