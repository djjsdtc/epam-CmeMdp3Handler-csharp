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

namespace Epam.CmeMdp3Handler.Core.Channel.Tcp
{
    /// <summary>
    /// Interface for TCP-based message gap recovery.
    ///
    /// Java: com.epam.cme.mdp3.core.channel.tcp.TCPMessageRequester
    /// </summary>
    public interface ITcpMessageRequester
    {
        /// <summary>Maximum number of messages available via TCP replay.</summary>
        const int MaxAvailableMessages = 2000;

        /// <summary>
        /// Requests lost messages from the TCP replay feed.
        /// </summary>
        /// <param name="beginSeqNo">First sequence number to recover</param>
        /// <param name="endSeqNo">Last sequence number to recover</param>
        /// <param name="tcpPacketListener">Listener to receive recovered packets</param>
        /// <returns>true if recovery succeeded</returns>
        bool AskForLostMessages(long beginSeqNo, long endSeqNo, ITcpPacketListener tcpPacketListener);
    }
}
