namespace Epam.CmeMdp3Handler.Core.Channel
{
    public interface ICoreChannelListener
    {
        /// <summary>Called when a feed has started receiving data.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="feedType">Type of feed (Incremental, Snapshot, Instrument)</param>
        /// <param name="feed">Feed A or B</param>
        void OnFeedStarted(string channelId, FeedType feedType, Feed feed);

        /// <summary>Called when a feed has stopped receiving data.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="feedType">Type of feed</param>
        /// <param name="feed">Feed A or B</param>
        void OnFeedStopped(string channelId, FeedType feedType, Feed feed);

        /// <summary>Called when a raw MDP packet is received on any feed.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="feedType">Type of feed</param>
        /// <param name="feed">Feed A or B</param>
        /// <param name="mdpPacket">The received packet</param>
        void OnPacket(string channelId, FeedType feedType, Feed feed, MdpPacket mdpPacket);

        /// <summary>Called when a Security Definition message is received.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="mdpMessage">The security definition message</param>
        /// <returns>Subscription flags to apply to the instrument</returns>
        int  OnSecurityDefinition(string channelId, IMdpMessage mdpMessage);

        /// <summary>Called just before a channel reset is processed.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="mdpMessage">The channel reset message</param>
        void OnBeforeChannelReset(string channelId, IMdpMessage mdpMessage);

        /// <summary>Called after a channel reset has been fully processed.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="mdpMessage">The channel reset message</param>
        void OnFinishedChannelReset(string channelId, IMdpMessage mdpMessage);

        /// <summary>Called when the channel state changes.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="prevState">Previous channel state</param>
        /// <param name="newState">New channel state</param>
        void OnChannelStateChanged(string channelId, ChannelState prevState, ChannelState newState);

        /// <summary>Called when a Request For Quote message is received.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="rfqMessage">The RFQ message</param>
        void OnRequestForQuote(string channelId, IMdpMessage rfqMessage);

        /// <summary>Called when a Security Status message is received.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="secStatusMessage">The security status message</param>
        void OnSecurityStatus(string channelId, int securityId, IMdpMessage secStatusMessage);
    }
}
