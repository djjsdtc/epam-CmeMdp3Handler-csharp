namespace Epam.CmeMdp3Handler.MktData
{
    using Epam.CmeMdp3Handler.MktData.Enums;
    using System.Collections.Generic;

    public interface IPublicTrades
    {
        int GetSecurityId();
        int ElectronicVolume();
        List<PublicTradeEntity> NewTrades();
        List<PublicTradeEntity> AdjustTrades();
    }
}