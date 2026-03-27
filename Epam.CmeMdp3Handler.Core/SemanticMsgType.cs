namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Market Data Application Messages, from business point of view.
    /// </summary>
    public enum SemanticMsgType
    {
        MarketDataIncrementalRefresh,
        MarketDataSnapshotFullRefresh,
        SecurityDefinition,
        SecurityStatus,
        QuoteRequest,
        Heartbeat,
        Logon,
        Logout
    }

    public static class SemanticMsgTypeExtensions
    {
        public static SemanticMsgType? FromFixValue(string? fixValue)
        {
            if (fixValue == null) return null;
            switch (fixValue)
            {
                case "X": return SemanticMsgType.MarketDataIncrementalRefresh;
                case "W": return SemanticMsgType.MarketDataSnapshotFullRefresh;
                case "d": return SemanticMsgType.SecurityDefinition;
                case "f": return SemanticMsgType.SecurityStatus;
                case "R": return SemanticMsgType.QuoteRequest;
                case "0": return SemanticMsgType.Heartbeat;
                case "A": return SemanticMsgType.Logon;
                case "5": return SemanticMsgType.Logout;
                default: return null;
            }
        }
    }
}
