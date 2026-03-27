namespace Epam.CmeMdp3Handler.MktData
{
    public interface IImpliedBook
    {
        const int PlatformImpliedBookDepth = 2;

        int GetSecurityId();
        IImpliedBookPriceLevel GetBid(byte level);
        IImpliedBookPriceLevel GetOffer(byte level);
    }
}
