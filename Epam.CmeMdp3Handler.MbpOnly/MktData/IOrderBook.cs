namespace Epam.CmeMdp3Handler.MktData
{
    public interface IOrderBook
    {
        int GetSecurityId();
        byte GetDepth();
        IOrderBookPriceLevel GetBid(byte level);
        IOrderBookPriceLevel GetOffer(byte level);
    }
}
