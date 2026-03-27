namespace Epam.CmeMdp3Handler.MktData
{
    public interface ISecurityStatistics
    {
        Price OpeningPrice();
        Price SettlementPrice();
        Price TradingSessionHighPrice();
        Price TradingSessionLowPrice();
    }
}
