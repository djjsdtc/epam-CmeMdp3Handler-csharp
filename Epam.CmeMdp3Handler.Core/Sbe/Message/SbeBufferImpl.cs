using System;
using System.Buffers.Binary;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    // Managed byte[] based SBE buffer using little-endian reads via BinaryPrimitives.
    // Replaces Java's Chronicle Bytes BytesStore<?,?> implementation.
    public class SbeBufferImpl : ISbeBuffer
    {
        private byte[] _data;
        private int _offset;
        private int _length;
        private int _position;

        public SbeBufferImpl()
        {
            _data = Array.Empty<byte>();
        }

        public SbeBufferImpl(int capacity)
        {
            _data = new byte[capacity];
        }

        public void Wrap(ISbeBuffer sb)
        {
            // can cast because the handler always use one implementation of buffer in runtime
            var buf = (SbeBufferImpl)sb;
            _data = buf._data;
            _offset = buf._offset;
            _length = buf._length;
            _position = buf._position;
        }

        public void WrapForParse(byte[] data, int length)
        {
            _data = data;
            _offset = 0;
            _length = length;
            _position = 0;
        }

        public void CopyTo(ISbeBuffer dest)
        {
            var d = (SbeBufferImpl)dest;
            if (d._data.Length < _length)
                d._data = new byte[_length];
            Buffer.BlockCopy(_data, 0, d._data, 0, _length);
            d._length = _length;
        }

        public void CopyTo(int offset, byte[] dest, int len)
        {
            Buffer.BlockCopy(_data, offset, dest, 0, len);
        }

        public void CopyFrom(ISbeBuffer src)
        {
            var s = (SbeBufferImpl)src;
            if (_data.Length < s._length)
                _data = new byte[s._length];
            Buffer.BlockCopy(s._data, 0, _data, 0, s._length);
            _length = s._length;
        }

        public ISbeBuffer Copy()
        {
            var copy = new SbeBufferImpl();
            CopyTo(copy);
            copy._offset = _offset;
            copy._length = _length;
            copy._position = _position;
            return copy;
        }

        public void Release() { /* no-op for managed memory */ }

        public int Offset() => _offset;
        public ISbeBuffer Offset(int offset) { _offset = offset; return this; }
        public int Length() => _length;
        public ISbeBuffer Length(int length) { _length = length; return this; }
        public int Position() => _position;
        public ISbeBuffer Position(int pos) { _position = _offset + pos; return this; }

        // Ensure internal buffer is large enough
        private void EnsureCapacity(int needed)
        {
            if (_data.Length < needed)
            {
                var newData = new byte[needed];
                Buffer.BlockCopy(_data, 0, newData, 0, _data.Length);
                _data = newData;
            }
        }

        public char GetChar() => (char)_data[_position];

        public short GetUInt8() => (short)(_data[_position] & 0xFF);

        public sbyte GetInt8() => _data[_position].ToSignedByte();

        public short GetInt16() => BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(_position));

        public int GetUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_position));

        public int GetInt32() => BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(_position));

        public long GetUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_position));

        public long GetInt64() => BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(_position));

        // UInt64 returned as long (same bit pattern); caller interprets as unsigned when needed
        public long GetUInt64() => BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(_position));

        public bool IsUInt64Null()
        {
            for (int i = 0; i < 8; i++)
            {
                if (_data[_position + i] != 0xFF) return false;
            }
            return true;
        }

        public void GetChars(char[] chars, int len)
        {
            if (len > chars.Length)
                throw new ArgumentException($"Char array length {chars.Length} less than requested {len}");
            for (int i = 0; i < len; i++)
                chars[i] = (char)_data[_position + i];
        }

        // Expose raw data for internal use (e.g., IncrementalRefreshHolder copy)
        internal byte[] RawData => _data;
        internal void SetData(byte[] data, int length)
        {
            _data = data;
            _length = length;
        }

        public override string ToString() =>
            $"SbeBufferImpl{{offset={_offset}, length={_length}, position={_position}}}";
    }
}
