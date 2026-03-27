namespace Epam.CmeMdp3Handler.MktData.Enums
{
    public enum SecurityTradingEvent
    {
        NoEvent           = 0,
        NoCancel          = 1,
        ResetStatistics   = 4,
        ImpliedMatchingON = 5,
        ImpliedMatchingOFF= 6
    }

    public static class SecurityTradingEventExtensions
    {
        public static SecurityTradingEvent FromFIX(byte fixValue) => fixValue switch
        {
            0 => SecurityTradingEvent.NoEvent,
            1 => SecurityTradingEvent.NoCancel,
            4 => SecurityTradingEvent.ResetStatistics,
            5 => SecurityTradingEvent.ImpliedMatchingON,
            6 => SecurityTradingEvent.ImpliedMatchingOFF,
            _ => SecurityTradingEvent.NoEvent
        };
    }
}
