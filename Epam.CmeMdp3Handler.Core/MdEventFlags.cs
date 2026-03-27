namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// High level subscription options to market data and events.
    /// </summary>
    public static class MdEventFlags
    {
        public const int NOTHING        = 0;
        public const int MESSAGE        = 1;
        public const int BOOK           = 1024;
        public const int TOP            = 2048;
        public const int IMPLIED_BOOK   = 4096;
        public const int IMPLIED_TOP    = 8192;
        public const int TRADE_SUMMARY  = 16384;
        public const int STATISTICS     = 32768;

        /// <summary>Returns true if no flags are set.</summary>
        public static bool IsNothing(int flags)           => flags == NOTHING;

        /// <summary>Returns true if the MESSAGE flag is set.</summary>
        public static bool HasMessage(int flags)          => (flags & MESSAGE) == MESSAGE;

        /// <summary>Returns true if the full order book subscription flag is set.</summary>
        public static bool HasBook(int flags)             => (flags & BOOK) == BOOK;

        /// <summary>Returns true if the top-of-book subscription flag is set.</summary>
        public static bool HasTop(int flags)              => (flags & TOP) == TOP;

        /// <summary>Returns true if the implied order book subscription flag is set.</summary>
        public static bool HasImpliedBook(int flags)      => (flags & IMPLIED_BOOK) == IMPLIED_BOOK;

        /// <summary>Returns true if the top-of-implied-book subscription flag is set.</summary>
        public static bool HasImpliedTop(int flags)       => (flags & IMPLIED_TOP) == IMPLIED_TOP;

        /// <summary>Returns true if the trade summary subscription flag is set.</summary>
        public static bool HasTradeSummary(int flags)     => (flags & TRADE_SUMMARY) == TRADE_SUMMARY;

        /// <summary>Returns true if the statistics subscription flag is set.</summary>
        public static bool HasStatistics(int flags)       => (flags & STATISTICS) == STATISTICS;
    }
}
