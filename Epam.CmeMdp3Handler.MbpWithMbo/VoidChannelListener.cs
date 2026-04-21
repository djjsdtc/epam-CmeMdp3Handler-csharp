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

namespace Epam.CmeMdp3Handler.MbpWithMbo
{
    /// <summary>
    /// Default no-op Channel Listener for MBP-with-MBO mode.
    /// Extend this class and override only the callbacks you need.
    ///
    /// Java: com.epam.cme.mdp3.VoidChannelListener (interface with default methods)
    /// C# note: Java interface default methods are not directly translatable; an abstract
    /// base class with virtual no-op implementations provides identical ergonomics.
    /// </summary>
    public abstract class VoidChannelListener : IChannelListener
    {
        public virtual void OnFeedStarted(string channelId, FeedType feedType, Feed feed) { }

        public virtual void OnFeedStopped(string channelId, FeedType feedType, Feed feed) { }

        public virtual void OnPacket(string channelId, FeedType feedType, Feed feed, MdpPacket mdpPacket) { }

        public virtual void OnBeforeChannelReset(string channelId, IMdpMessage resetMessage) { }

        public virtual void OnFinishedChannelReset(string channelId, IMdpMessage resetMessage) { }

        public virtual void OnChannelStateChanged(string channelId, ChannelState prevState, ChannelState newState) { }

        public virtual int OnSecurityDefinition(string channelId, IMdpMessage mdpMessage) => MdEventFlags.NOTHING;

        public virtual void OnRequestForQuote(string channelId, IMdpMessage rfqMessage) { }

        public virtual void OnSecurityStatus(string channelId, int securityId, IMdpMessage secStatusMessage) { }

        public virtual void OnIncrementalMBORefresh(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum, IFieldSet orderEntry, IFieldSet? mdEntry) { }

        public virtual void OnIncrementalMBPRefresh(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum, IFieldSet mdEntry) { }

        public virtual void OnIncrementalComplete(IMdpMessage mdpMessage, string channelId, int securityId, string? secDesc, long msgSeqNum) { }

        public virtual void OnSnapshotMBOFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage) { }

        public virtual void OnSnapshotMBPFullRefresh(string channelId, string? secDesc, IMdpMessage snptMessage) { }
    }
}
