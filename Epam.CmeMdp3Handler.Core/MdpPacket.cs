using System;
using System.Collections;
using System.Collections.Generic;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// MDP Packet. Contains methods to create and to iterate MDP Messages in it.
    /// </summary>
    public class MdpPacket : IEnumerable<IMdpMessage>
    {
        private ISbeBuffer _sbeBuffer;
        private readonly MdpMessageIterator _iterator;
        private readonly SbeMessage _sbeMessage;

        public MdpPacket()
        {
            _sbeBuffer = new SbeBufferImpl(SbeConstants.MDP_PACKET_MAX_SIZE);
            _sbeMessage = new SbeMessage();
            _iterator = new MdpMessageIterator(this);
        }

        public MdpPacket(ISbeBuffer sbeBuffer)
        {
            _sbeBuffer = sbeBuffer;
            _sbeMessage = new SbeMessage();
            _iterator = new MdpMessageIterator(this);
        }

        /// <summary>Creates a new MDP packet instance with a default-sized buffer.</summary>
        public static MdpPacket Instance() => new MdpPacket();

        /// <summary>Allocates a new MDP packet with the maximum packet buffer size.</summary>
        public static MdpPacket Allocate() => Allocate(SbeConstants.MDP_PACKET_MAX_SIZE);

        /// <summary>Allocates a new MDP packet with the given buffer size.</summary>
        /// <param name="size">Buffer size in bytes</param>
        public static MdpPacket Allocate(int size)
        {
            var packet = Instance();
            var buf = new SbeBufferImpl(size);
            packet.WrapFromBuffer(buf);
            return packet;
        }

        /// <summary>Creates a deep copy of this packet.</summary>
        public MdpPacket Copy()
        {
            var copy = Allocate(Buffer().Length());
            copy.Buffer().CopyFrom(Buffer());
            return copy;
        }

        /// <summary>Returns the underlying SBE buffer.</summary>
        public ISbeBuffer Buffer() => _sbeBuffer;

        /// <summary>Wraps this packet around an existing SBE buffer.</summary>
        public void WrapFromBuffer(ISbeBuffer sbeBuffer) { _sbeBuffer = sbeBuffer; }

        /// <summary>Wraps the internal buffer with data from a byte array.</summary>
        public void WrapFromBuffer(byte[] data, int length)
        {
            _sbeBuffer.WrapForParse(data, length);
        }

        /// <summary>Returns the total byte length of this packet.</summary>
        public int GetPacketSize() => _sbeBuffer.Length();

        /// <summary>Returns the message sequence number from the packet header.</summary>
        public long GetMsgSeqNum()
        {
            _sbeBuffer.Position(SbeConstants.MESSAGE_SEQ_NUM_OFFSET);
            return _sbeBuffer.GetUInt32();
        }

        /// <summary>Returns the sending time (nanoseconds since epoch) from the packet header.</summary>
        public long GetSendingTime()
        {
            _sbeBuffer.Position(SbeConstants.MESSAGE_SENDING_TIME_OFFSET);
            return _sbeBuffer.GetUInt64();
        }

        public MdpPacket Length(int length)
        {
            _sbeBuffer.Length(length);
            return this;
        }

        /// <summary>Releases the underlying buffer resources.</summary>
        public void Release() => Buffer().Release();

        public IEnumerator<IMdpMessage> GetEnumerator()
        {
            _iterator.Init(Buffer().Offset() + SbeConstants.MDP_HEADER_SIZE, Buffer().Length() - SbeConstants.MDP_HEADER_SIZE);
            return _iterator;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => $"MdpPacket{{msgSeqNum={GetMsgSeqNum()}, buffer={Buffer()}}}";

        private class MdpMessageIterator : IEnumerator<IMdpMessage>
        {
            private readonly MdpPacket _packet;
            private int _offset;         // offset of the message start in the byteBuffer
            private int _packetMaxOffset; // the max packet offset in the byteBuffer
            private IMdpMessage? _current;

            public MdpMessageIterator(MdpPacket packet) { _packet = packet; }

            public void Init(int offset, int packetSize)
            {
                _offset = offset;
                _packetMaxOffset = offset + packetSize;
            }

            public IMdpMessage Current => _current!;
            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                if (_offset >= _packetMaxOffset) return false;
                var msg = _packet._sbeMessage;
                msg.Buffer().Wrap(_packet._sbeBuffer);
                msg.Buffer().Offset(_offset);
                int messageLength = msg.GetMsgSize();
                msg.Buffer().Length(messageLength);
                _offset += messageLength;
                _current = msg;
                return true;
            }

            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }
}
