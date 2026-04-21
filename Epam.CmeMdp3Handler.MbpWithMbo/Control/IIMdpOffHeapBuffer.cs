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

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Interface for the packet buffer used during gap recovery.
    ///
    /// Java: com.epam.cme.mdp3.control.IMDPOffHeapBuffer
    /// C# note: Interface prefixed with I per .NET conventions.
    ///          Name kept as IIMdpOffHeapBuffer to maintain the double-I only in the
    ///          type name (IMDPOffHeapBuffer -> IIMdpOffHeapBuffer). Alternatively
    ///          named IMdpOffHeapBuffer here for cleaner C# style.
    /// </summary>
    public interface IMdpOffHeapBuffer
    {
        bool Exist(long msgSeqNum);
        MdpPacket? Remove(long msgSeqNum);
        void Add(long msgSeqNum, MdpPacket packet);
        long GetLastMsgSeqNum();
        void Clear();
        void Clear(long msgSeqNum);
    }
}
