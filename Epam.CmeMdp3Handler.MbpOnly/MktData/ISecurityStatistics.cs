namespace Epam.CmeMdp3Handler.MktData
{
    public interface ISecurityStatistics
    {
        double? OpeningPrice();
        double? SettlementPrice();
        double? TradingSessionHighPrice();
        double? TradingSessionLowPrice();
    }
}
