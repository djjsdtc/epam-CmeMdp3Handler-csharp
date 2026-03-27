using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    public interface IChannelListener : ICoreChannelListener
    {
        /// <summary>Called when an instrument's state changes.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description, if available</param>
        /// <param name="prevState">Previous instrument state</param>
        /// <param name="newState">New instrument state</param>
        void OnInstrumentStateChanged(string channelId, int securityId, string? secDesc,
            InstrumentState prevState, InstrumentState newState);

        /// <summary>Called for each incremental refresh entry received.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="matchEventIndicator">Bitmap indicating event boundaries</param>
        /// <param name="securityId">Security ID of the entry</param>
        /// <param name="secDesc">Security description, if available</param>
        /// <param name="msgSeqNum">Message sequence number</param>
        /// <param name="mdpGroupEntry">The group entry containing the incremental update fields</param>
        void OnIncrementalRefresh(string channelId, short matchEventIndicator,
            int securityId, string? secDesc, long msgSeqNum, IFieldSet mdpGroupEntry);

        /// <summary>Called when a snapshot full refresh message is received for a security.</summary>
        /// <param name="channelId">Channel ID</param>
        /// <param name="secDesc">Security description, if available</param>
        /// <param name="mdpMessage">The full snapshot message</param>
        void OnSnapshotFullRefresh(string channelId, string? secDesc, IMdpMessage mdpMessage);
    }
}
