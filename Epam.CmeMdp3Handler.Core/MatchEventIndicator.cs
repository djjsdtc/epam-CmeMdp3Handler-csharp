namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Bitmap field indicating the business event boundary for MDP incremental refresh messages.
    /// Each bit indicates the last message of the corresponding event type within the packet:
    /// <list type="bullet">
    /// <item><description>Bit 0 (1): LastTradeMsg   — last trade message in event</description></item>
    /// <item><description>Bit 1 (2): LastVolumeMsg  — last volume message in event</description></item>
    /// <item><description>Bit 2 (4): LastQuoteMsg   — last quote message in event</description></item>
    /// <item><description>Bit 3 (8): LastStatsMsg   — last statistics message in event</description></item>
    /// <item><description>Bit 4 (16): LastImpliedMsg — last implied price message in event</description></item>
    /// <item><description>Bit 5 (32): RecoveryMsg   — message is part of a recovery</description></item>
    /// <item><description>Bit 6 (64): Reserved</description></item>
    /// <item><description>Bit 7 (128): EndOfEvent   — last message in the match event</description></item>
    /// </list>
    /// </summary>
    public static class MatchEventIndicator
    {
        public const short NOTHING         = 0;
        public const short LASTTRADEMSG    = 1;
        public const short LASTVOLUMEMSG   = 2;
        public const short LASTQUOTEMSG    = 4;
        public const short LASTSTATSMSG    = 8;
        public const short LASTIMPLIEDMSG  = 16;
        public const short RECOVERYMSG     = 32;
        public const short RESERVED        = 64;
        public const short ENDOFEVENT      = 128;

        public static bool HasLastTradeMsg(short indicator)   => (indicator & LASTTRADEMSG) == LASTTRADEMSG;
        public static bool HasLastVolumeMsg(short indicator)  => (indicator & LASTVOLUMEMSG) == LASTVOLUMEMSG;
        public static bool HasLastQuoteMsg(short indicator)   => (indicator & LASTQUOTEMSG) == LASTQUOTEMSG;
        public static bool HasLastStatsMsg(short indicator)   => (indicator & LASTSTATSMSG) == LASTSTATSMSG;
        public static bool HasLastImpliedMsg(short indicator) => (indicator & LASTIMPLIEDMSG) == LASTIMPLIEDMSG;
        public static bool HasRecoveryMsg(short indicator)    => (indicator & RECOVERYMSG) == RECOVERYMSG;
        public static bool HasEndOfEvent(short indicator)     => (indicator & ENDOFEVENT) == ENDOFEVENT;
    }
}
