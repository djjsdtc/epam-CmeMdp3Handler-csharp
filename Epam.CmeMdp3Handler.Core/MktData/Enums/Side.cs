namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum Side
    {
        Buy = 1,
        Sell = 2,
        Cross = 8
    }

    public static class SideExtensions
    {
        private const sbyte NULL_VALUE = 127; // Int8 null

        public static Side? FromFIX(sbyte fixValue) => fixValue switch
        {
            NULL_VALUE => null,
            1 => Side.Buy,
            2 => Side.Sell,
            8 => Side.Cross,
            _ => null
        };
    }
}
