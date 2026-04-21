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

using System.Collections.Generic;
using Epam.CmeMdp3Handler.Core.Channel;

namespace Epam.CmeMdp3Handler.MbpWithMbo
{
    /// <summary>
    /// Interface to MDP Channel with its lifecycle and all included Feeds inside too.
    ///
    /// Java: com.epam.cme.mdp3.MdpChannel
    /// C# note: Interface prefixed with I per .NET conventions.
    /// </summary>
    public interface IMdpChannel
    {
        /// <summary>
        /// Gets ID of MDP Channel.
        /// </summary>
        /// <returns>Channel ID</returns>
        string GetId();

        /// <summary>
        /// Closes MDP Channel and releases all resources.
        /// </summary>
        void Close();

        /// <summary>
        /// Gets current State of the channel.
        /// </summary>
        /// <returns>Channel State</returns>
        ChannelState GetState();

        /// <summary>
        /// Registers Channel Listener.
        /// </summary>
        /// <param name="channelListener">Instance of Channel Listener</param>
        void RegisterListener(IChannelListener channelListener);

        /// <summary>
        /// Removes Channel Listener.
        /// </summary>
        /// <param name="channelListener">Instance of Channel Listener</param>
        void RemoveListener(IChannelListener channelListener);

        /// <summary>
        /// Gets all registered Channel Listeners.
        /// </summary>
        /// <returns>List of IChannelListeners</returns>
        IList<IChannelListener> GetListeners();

        /// <summary>
        /// Starts defined feed.
        /// </summary>
        /// <param name="feedType">Type of feed</param>
        /// <param name="feed">Feed (A or B)</param>
        void StartFeed(FeedType feedType, Feed feed);

        /// <summary>
        /// Stops defined feed.
        /// </summary>
        /// <param name="feedType">Type of feed</param>
        /// <param name="feed">Feed (A or B)</param>
        void StopFeed(FeedType feedType, Feed feed);

        /// <summary>
        /// Stops all Feeds.
        /// </summary>
        void StopAllFeeds();

        /// <summary>
        /// Subscribes to the given security.
        /// </summary>
        /// <param name="securityId">Security ID</param>
        /// <param name="secDesc">Security description</param>
        /// <returns>true if subscribed with success</returns>
        bool Subscribe(int securityId, string? secDesc);

        /// <summary>
        /// Removes subscription to the given security.
        /// </summary>
        /// <param name="securityId">Security ID</param>
        void DiscontinueSecurity(int securityId);
    }
}
