using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Default Channel Listener without any activity in all callbacks.
    /// Can be used to easily create a user application listener by extending this default
    /// listener and overriding only required callbacks.
    /// Abstract base class providing no-op implementations of all IChannelListener methods.
    /// Java used interface default methods; C# uses an abstract base class.
    /// </summary>
    public abstract class VoidChannelListener : IChannelListener
    {
        public virtual void OnFeedStarted(string channelId, FeedType feedType, Feed feed) { }
        public virtual void OnFeedStopped(string channelId, FeedType feedType, Feed feed) { }
        public virtual void OnPacket(string channelId, FeedType feedType, Feed feed, MdpPacket mdpPacket) { }
        public virtual int  OnSecurityDefinition(string channelId, IMdpMessage mdpMessage) => MdEventFlags.NOTHING;
        public virtual void OnBeforeChannelReset(string channelId, IMdpMessage mdpMessage) { }
        public virtual void OnFinishedChannelReset(string channelId, IMdpMessage mdpMessage) { }
        public virtual void OnChannelStateChanged(string channelId, ChannelState prevState, ChannelState newState) { }
        public virtual void OnRequestForQuote(string channelId, IMdpMessage rfqMessage) { }
        public virtual void OnSecurityStatus(string channelId, int securityId, IMdpMessage secStatusMessage) { }
        public virtual void OnInstrumentStateChanged(string channelId, int securityId, string? secDesc,
            InstrumentState prevState, InstrumentState newState) { }
        public virtual void OnIncrementalRefresh(string channelId, short matchEventIndicator,
            int securityId, string? secDesc, long msgSeqNum, IFieldSet mdpGroupEntry) { }
        public virtual void OnSnapshotFullRefresh(string channelId, string? secDesc, IMdpMessage mdpMessage) { }
    }
}
