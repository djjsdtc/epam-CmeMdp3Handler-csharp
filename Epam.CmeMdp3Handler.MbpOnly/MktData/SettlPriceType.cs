namespace Epam.CmeMdp3Handler.MktData
{
    using System;

    [Flags]
    public enum SettlPriceType
    {
        Nothing = 0,
        Final = 1,
        Actual = 1 << 1,
        Rounded = 1 << 2,
        Intraday = 1 << 3,
        NullValue = 1 << 7,
    }
}
