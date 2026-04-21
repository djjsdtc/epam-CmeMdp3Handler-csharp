/*
 * Copyright 2004-2016 EPAM Systems
 * This file is part of Java Market Data Handler for CME Market Data (MDP 3.0).
 * Java Market Data Handler for CME Market Data (MDP 3.0) is free software: you can redistribute it and/or modify it under the terms of the
 * GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Java Market Data Handler for CME Market Data (MDP 3.0) is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Java Market Data Handler for CME Market Data (MDP 3.0).
 * If not, see <http://www.gnu.org/licenses/>.
 */

using Epam.CmeMdp3Handler.Core.Channel;

namespace Epam.CmeMdp3Handler.MbpWithMbo
{
    /// <summary>
    /// Channel listener interface for MBP-with-MBO mode.
    /// Extends <see cref="ICoreChannelListener"/> with MBO- and MBP-specific callbacks.
    ///
    /// Java: com.epam.cme.mdp3.ChannelListener extends CoreChannelListener
    /// C# note: Interface prefixed with I per .NET conventions.
    /// </summary>
    public interface IChannelListener : ICoreChannelListener
    {
        /// <summary>
        /// Called when MDP Incremental Refresh Message is received and Security-related entry is processed.
        ///
        /// Only when MBO is enabled.
        /// </summary>
        /// <param name="mdpMessage">The full MDP Message</param>
        /// <param name="channelId">ID of MDP Channel</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description</param>
        /// <param name="msgSeqNum">Message sequence number of message.</param>
        /// <param name="orderEntry">MBO Entry of Group from MDP Incremental Refresh Message. It can not be null.</param>
        /// <param name="mdEntry">MBP Entry of Group from MDP Incremental Refresh Message. It can be null when MBO Incremental Refresh is received in separated template.</param>
        void OnIncrementalMBORefresh(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum, IFieldSet orderEntry, IFieldSet? mdEntry);

        /// <summary>
        /// Called when MDP Incremental Refresh Message is received with MBP entry.
        /// </summary>
        /// <param name="mdpMessage">The full MDP Message</param>
        /// <param name="channelId">ID of MDP Channel</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description</param>
        /// <param name="msgSeqNum">Message sequence number of message.</param>
        /// <param name="mdEntry">MBP Entry of Group from MDP Incremental Refresh Message. It can not be null.</param>
        void OnIncrementalMBPRefresh(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum, IFieldSet mdEntry);

        /// <summary>
        /// Called when a Incremental MsgSeqNum has been fully processed.
        /// This callback will be called for each securityId found in the MsgSeqNum packet.
        /// </summary>
        /// <param name="mdpMessage">The full MDP Message</param>
        /// <param name="channelId">ID of MDP Channel</param>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description</param>
        /// <param name="msgSeqNum">Message sequence number of message.</param>
        void OnIncrementalComplete(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum);

        /// <summary>
        /// Called when MDP Snapshot Full Refresh Message for MBO is received and processed.
        /// </summary>
        /// <param name="channelId">ID of MDP Channel</param>
        /// <param name="secDesc">Security description</param>
        /// <param name="snptMessage">MDP Snapshot Full Refresh Message for MBO</param>
        void OnSnapshotMBOFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage);

        /// <summary>
        /// Called when MDP Snapshot Full Refresh Message is received and processed.
        /// </summary>
        /// <param name="channelId">ID of MDP Channel</param>
        /// <param name="secDesc">Security description</param>
        /// <param name="snptMessage">MDP Snapshot Full Refresh Message for MBP</param>
        void OnSnapshotMBPFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage);
    }
}
