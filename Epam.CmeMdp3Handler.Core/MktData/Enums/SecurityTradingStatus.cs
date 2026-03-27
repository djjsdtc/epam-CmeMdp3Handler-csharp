namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum SecurityTradingStatus
    {
        TradingHalt       = 2,
        Close             = 4,
        NewPriceIndication = 15,
        ReadyToTrade      = 17,
        NotAvailableForTrading = 18,
        UnknownOrInvalid  = 20,
        PreOpen           = 21,
        PreCross          = 24,
        Cross             = 25,
        PostClose         = 26,
        NoChange          = 103
    }

    public static class SecurityTradingStatusExtensions
    {
        public static SecurityTradingStatus? FromFIX(short fixValue) => fixValue switch
        {
            2   => SecurityTradingStatus.TradingHalt,
            4   => SecurityTradingStatus.Close,
            15  => SecurityTradingStatus.NewPriceIndication,
            17  => SecurityTradingStatus.ReadyToTrade,
            18  => SecurityTradingStatus.NotAvailableForTrading,
            20  => SecurityTradingStatus.UnknownOrInvalid,
            21  => SecurityTradingStatus.PreOpen,
            24  => SecurityTradingStatus.PreCross,
            25  => SecurityTradingStatus.Cross,
            26  => SecurityTradingStatus.PostClose,
            103 => SecurityTradingStatus.NoChange,
            _   => null
        };
    }
}
