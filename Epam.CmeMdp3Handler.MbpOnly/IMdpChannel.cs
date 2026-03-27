using System.Collections.Generic;
using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to MDP Channel with its lifecycle and all included Feeds inside too.
    /// </summary>
    public interface IMdpChannel
    {
        /// <summary>Returns the channel ID.</summary>
        string GetId();

        /// <summary>Closes the channel and stops all feeds.</summary>
        void Close();

        /// <summary>Enables all-securities mode: any security seen on the feed is automatically tracked.</summary>
        void EnableAllSecuritiesMode();

        /// <summary>Disables all-securities mode.</summary>
        void DisableAllSecuritiesMode();

        /// <summary>Returns the default maximum order book depth for new subscriptions.</summary>
        byte GetDefMaxBookDepth();

        /// <summary>Sets the default maximum order book depth for new subscriptions.</summary>
        /// <param name="defMaxBookDepth">Maximum book depth</param>
        void SetDefMaxBookDepth(byte defMaxBookDepth);

        /// <summary>Returns the default subscription options flags for new subscriptions.</summary>
        int GetDefSubscriptionOptions();

        /// <summary>Sets the default subscription options flags for new subscriptions.</summary>
        /// <param name="defSubscriptionOptions">Subscription flags from <see cref="MdEventFlags"/></param>
        void SetDefSubscriptionOptions(int defSubscriptionOptions);

        /// <summary>Returns the current channel state.</summary>
        ChannelState GetState();

        /// <summary>Forcibly sets the channel state (for testing or recovery).</summary>
        /// <param name="state">New channel state</param>
        void SetStateForcibly(ChannelState state);

        /// <summary>Registers a channel event listener.</summary>
        /// <param name="channelListener">Listener to register</param>
        void RegisterListener(IChannelListener channelListener);

        /// <summary>Removes a previously registered channel event listener.</summary>
        /// <param name="channelListener">Listener to remove</param>
        void RemoveListener(IChannelListener channelListener);

        /// <summary>Registers a market data listener for high-level book/trade events.</summary>
        /// <param name="mdListener">Listener to register</param>
        void RegisterMarketDataListener(IMarketDataListener mdListener);

        /// <summary>Removes a previously registered market data listener.</summary>
        /// <param name="mdListener">Listener to remove</param>
        void RemoveMarketDataListener(IMarketDataListener mdListener);

        /// <summary>Returns all registered channel listeners.</summary>
        List<IChannelListener> GetListeners();

        /// <summary>Returns all registered market data listeners.</summary>
        List<IMarketDataListener> GetMdListeners();

        /// <summary>Starts the Incremental Feed A receive thread.</summary>
        void StartIncrementalFeedA();

        /// <summary>Starts the Incremental Feed B receive thread.</summary>
        void StartIncrementalFeedB();

        /// <summary>Starts the Snapshot Feed A receive thread.</summary>
        void StartSnapshotFeedA();

        /// <summary>Starts the Snapshot Feed B receive thread.</summary>
        void StartSnapshotFeedB();

        /// <summary>Starts the Instrument (Security Definition) Feed A receive thread.</summary>
        void StartInstrumentFeedA();

        /// <summary>Starts the Instrument (Security Definition) Feed B receive thread.</summary>
        void StartInstrumentFeedB();

        /// <summary>Stops the Incremental Feed A.</summary>
        void StopIncrementalFeedA();

        /// <summary>Stops the Incremental Feed B.</summary>
        void StopIncrementalFeedB();

        /// <summary>Stops the Snapshot Feed A.</summary>
        void StopSnapshotFeedA();

        /// <summary>Stops the Snapshot Feed B.</summary>
        void StopSnapshotFeedB();

        /// <summary>Stops the Instrument Feed A.</summary>
        void StopInstrumentFeedA();

        /// <summary>Stops the Instrument Feed B.</summary>
        void StopInstrumentFeedB();

        /// <summary>Stops all active feeds.</summary>
        void StopAllFeeds();

        /// <summary>Starts the instrument (security definition) feeds.</summary>
        void StartInstrumentFeeds();

        /// <summary>Starts the snapshot feeds.</summary>
        void StartSnapshotFeeds();

        /// <summary>Stops the snapshot feeds.</summary>
        void StopSnapshotFeeds();

        /// <summary>Subscribes to market data for a security with explicit flags and depth.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description (optional)</param>
        /// <param name="subscrFlags">Subscription flags from <see cref="MdEventFlags"/></param>
        /// <param name="depth">Maximum order book depth</param>
        /// <returns>True if subscription was registered</returns>
        bool Subscribe(int securityId, string? secDesc, int subscrFlags, byte depth);

        /// <summary>Subscribes to market data using default book depth.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description (optional)</param>
        /// <param name="subscrFlags">Subscription flags from <see cref="MdEventFlags"/></param>
        /// <returns>True if subscription was registered</returns>
        bool SubscribeWithDefDepth(int securityId, string? secDesc, int subscrFlags);

        /// <summary>Subscribes to market data using default subscription options.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description (optional)</param>
        /// <param name="depth">Maximum order book depth</param>
        /// <returns>True if subscription was registered</returns>
        bool Subscribe(int securityId, string? secDesc, byte depth);

        /// <summary>Subscribes to market data using all defaults.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description (optional)</param>
        /// <returns>True if subscription was registered</returns>
        bool Subscribe(int securityId, string? secDesc);

        /// <summary>Removes a security from active tracking.</summary>
        /// <param name="securityId">Security ID to discontinue</param>
        void DiscontinueSecurity(int securityId);

        /// <summary>Returns the current subscription flags for a security.</summary>
        /// <param name="securityId">Security ID</param>
        int GetSubscriptionFlags(int securityId);

        /// <summary>Replaces the subscription flags for a security.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="flags">New subscription flags</param>
        void SetSubscriptionFlags(int securityId, int flags);

        /// <summary>Adds subscription flags to a security's existing flags.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="flags">Flags to add</param>
        void AddSubscriptionFlags(int securityId, int flags);

        /// <summary>Removes subscription flags from a security's existing flags.</summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="flags">Flags to remove</param>
        void RemoveSubscriptionFlags(int securityId, int flags);

        /// <summary>Handles an incoming MDP packet from a feed (used for manual/test injection).</summary>
        /// <param name="feedContext">Feed context indicating the feed type and side</param>
        /// <param name="mdpPacket">The MDP packet to process</param>
        void HandlePacket(MdpFeedContext feedContext, MdpPacket mdpPacket);
    }
}
