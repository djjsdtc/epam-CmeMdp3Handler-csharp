namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum Side
    {
        Buy   = 1,
        Sell  = 2,
        Cross = 8
    }

    public static class SideExtensions
    {
        private const byte NULL_VALUE = 127; // Int8 null

        public static Side? FromFIX(byte fixValue) => fixValue switch
        {
            NULL_VALUE => null,
            1 => Side.Buy,
            2 => Side.Sell,
            8 => Side.Cross,
            _ => null
        };
    }
}
