namespace Epam.CmeMdp3Handler.MktData
{
    public static class SettlPriceType
    {
        public const short Nothing = 0;
        public const short Final = 1;
        public const short Actual = 1 << 1;
        public const short Rounded = 1 << 2;
        public const short Intraday = 1 << 3;
        public const short NullValue = 1 << 7;

        public static bool HasIndicator(int flags, int flag) => (flags & flag) == flag;
        public static bool IsNothing(byte indicator) => indicator == Nothing;
        public static bool HasFinal(byte indicator) => HasIndicator(indicator, Final);
        public static bool HasActual(byte indicator) => HasIndicator(indicator, Actual);
        public static bool HasRounded(byte indicator) => HasIndicator(indicator, Rounded);
        public static bool HasIntraday(byte indicator) => HasIndicator(indicator, Intraday);
        public static bool HasNullValue(byte indicator) => HasIndicator(indicator, NullValue);
    }
}
