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

using System;
using System.Buffers.Binary;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Ring-buffer-style packet store used during gap recovery.
    ///
    /// Java: com.epam.cme.mdp3.control.MDPOffHeapBuffer
    /// C# note: Java uses NativeBytesStore (Chronicle Bytes / off-heap direct memory)
    ///          for zero-allocation packet storage. C# replaces this with a managed
    ///          MdpPacket[] ring buffer using byte[] copies via ISbeBuffer.CopyFrom.
    ///          The sentinel value Integer.MAX_VALUE (2147483647) is preserved.
    ///          The "empty" packet is initialised by writing the sentinel directly into
    ///          the first 4 bytes of the underlying byte[] (little-endian UInt32 at
    ///          MESSAGE_SEQ_NUM_OFFSET=0), matching the Java NativeBytesStore.writeUnsignedInt.
    /// </summary>
    public class MdpOffHeapBuffer : IMdpOffHeapBuffer
    {
        private const long UndefinedValue = int.MaxValue; // UNDEFINED_VALUE sentinel (Integer.MAX_VALUE)

        private readonly MdpPacket[] _data;
        private readonly MdpPacket _resultPacket = MdpPacket.Allocate();
        private readonly MdpPacket _emptyPacket;
        private long _lastMsgSeqNum = 0;

        public MdpOffHeapBuffer(int capacity)
        {
            // Build an "empty" packet whose MsgSeqNum == UndefinedValue
            _emptyPacket = MdpPacket.Allocate();
            WriteSentinel(_emptyPacket);

            _data = new MdpPacket[capacity];
            for (int i = 0; i < capacity; i++)
            {
                MdpPacket mdpPacket = MdpPacket.Allocate();
                Copy(_emptyPacket, mdpPacket);
                _data[i] = mdpPacket;
            }
        }

        public bool Exist(long msgSeqNum)
        {
            MdpPacket packet = _data[Index(msgSeqNum)];
            return !IsPacketEmpty(packet);
        }

        public MdpPacket? Remove(long msgSeqNum)
        {
            MdpPacket nextPacket = _data[Index(msgSeqNum)];
            if (IsPacketEmpty(nextPacket))
                return null;

            Copy(nextPacket, _resultPacket);
            Copy(_emptyPacket, nextPacket);
            return _resultPacket;
        }

        public void Add(long msgSeqNum, MdpPacket packet)
        {
            Copy(packet, _data[Index(msgSeqNum)]);
            if (msgSeqNum > _lastMsgSeqNum)
                _lastMsgSeqNum = msgSeqNum;
        }

        public long GetLastMsgSeqNum() => _lastMsgSeqNum;

        public void Clear()
        {
            for (int i = 0; i < _data.Length; i++)
                Copy(_emptyPacket, _data[i]);
            _lastMsgSeqNum = 0;
        }

        public void Clear(long msgSeqNum)
        {
            Copy(_emptyPacket, _data[Index(msgSeqNum)]);
        }

        private int Index(long msgSeqNum) => (int)(msgSeqNum % _data.Length);

        private static void Copy(MdpPacket from, MdpPacket to)
        {
            to.Buffer().CopyFrom(from.Buffer());
            to.Length(from.GetPacketSize());
        }

        private static bool IsPacketEmpty(MdpPacket mdpPacket) =>
            mdpPacket.GetMsgSeqNum() == UndefinedValue;

        /// <summary>
        /// Writes the sentinel value (Integer.MAX_VALUE as little-endian UInt32) at offset 0
        /// of the packet buffer, so that GetMsgSeqNum() returns UndefinedValue.
        /// </summary>
        private static void WriteSentinel(MdpPacket packet)
        {
            // Allocate a small byte[] with just the sentinel at position 0
            byte[] sentinel = new byte[SbeConstants.MDP_PACKET_MAX_SIZE];
            BinaryPrimitives.WriteUInt32LittleEndian(sentinel.AsSpan(SbeConstants.MESSAGE_SEQ_NUM_OFFSET), (uint)UndefinedValue);
            packet.Buffer().WrapForParse(sentinel, SbeConstants.MDP_PACKET_MAX_SIZE);
            packet.Length(SbeConstants.MDP_PACKET_MAX_SIZE);
        }
    }
}
