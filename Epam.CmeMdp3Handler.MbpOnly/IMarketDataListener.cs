using Epam.CmeMdp3Handler.MktData;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// High-level Market Data listener.
    /// </summary>
    public interface IMarketDataListener
    {
        /// <summary>Called when the top-of-book for the implied order book changes.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="bid">Best implied bid level</param>
        /// <param name="offer">Best implied offer level</param>
        void OnTopOfImpliedBookRefresh(string channelId, int securityId,
            IImpliedBookPriceLevel bid, IImpliedBookPriceLevel offer);

        /// <summary>Called when the top-of-book for the order book changes.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="bid">Best bid level</param>
        /// <param name="offer">Best offer level</param>
        void OnTopOfBookRefresh(string channelId, int securityId,
            IOrderBookPriceLevel bid, IOrderBookPriceLevel offer);

        /// <summary>Called when the implied order book is updated (incremental).</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="impliedBook">The implied order book snapshot</param>
        void OnImpliedBookRefresh(string channelId, int securityId, IImpliedBook impliedBook);

        /// <summary>Called when the implied order book is fully refreshed from a snapshot.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="impliedBook">The full implied order book</param>
        void OnImpliedBookFullRefresh(string channelId, int securityId, IImpliedBook impliedBook);

        /// <summary>Called when the order book is updated (incremental).</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="orderBook">The order book snapshot</param>
        void OnOrderBookRefresh(string channelId, int securityId, IOrderBook orderBook);

        /// <summary>Called when the order book is fully refreshed from a snapshot.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="orderBook">The full order book</param>
        void OnOrderBookFullRefresh(string channelId, int securityId, IOrderBook orderBook);

        /// <summary>Called when security statistics are updated.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="statistics">The updated statistics</param>
        void OnStatisticsRefresh(string channelId, int securityId, ISecurityStatistics statistics);

        /// <summary>Called when a Request For Quote is received for a security.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="quoReqId">Quote request ID</param>
        /// <param name="entryIdx">Index of this RFQ entry within the message</param>
        /// <param name="entryNum">Total number of RFQ entries in the message</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="quoteType">Type of quote requested</param>
        /// <param name="orderQty">Order quantity</param>
        /// <param name="side">Order side (buy/sell), if specified</param>
        void OnRequestForQuote(string channelId, SbeString quoReqId, int entryIdx, int entryNum,
            int securityId, QuoteType quoteType, int orderQty, Side? side);

        /// <summary>Called when a security status message is received.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="secGroup">Security group</param>
        /// <param name="secAsset">Security asset</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="tradeDate">Trade date</param>
        /// <param name="matchEventIndicator">Match event indicator bitmap</param>
        /// <param name="secTrdStatus">Trading status, if present</param>
        /// <param name="haltReason">Halt reason, if present</param>
        /// <param name="secTrdEvent">Trading event type</param>
        void OnSecurityStatus(string channelId, SbeString secGroup, SbeString secAsset,
            int securityId, int tradeDate, short matchEventIndicator,
            SecurityTradingStatus? secTrdStatus, HaltReason? haltReason, SecurityTradingEvent secTrdEvent);
    }
}
