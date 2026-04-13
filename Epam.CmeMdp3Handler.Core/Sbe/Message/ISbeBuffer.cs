namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public interface ISbeBuffer
    {
        void Wrap(ISbeBuffer sb);

        /// <summary>Wraps back buffer with data from byte buffer.</summary>
        /// <param name="data">Source byte array containing packet data</param>
        /// <param name="length">Number of valid bytes in data</param>
        void WrapForParse(byte[] data, int length);
        void CopyTo(ISbeBuffer dest);
        void CopyTo(int offset, byte[] dest, int len);
        void CopyFrom(ISbeBuffer src);
        ISbeBuffer Copy();
        void Release();

        int Offset();
        ISbeBuffer Offset(int offset);
        int Length();
        ISbeBuffer Length(int length);
        int Position();
        ISbeBuffer Position(int pos);

        char GetChar();
        short GetUInt8();
        sbyte GetInt8();
        short GetInt16();
        int GetUInt16();
        int GetInt32();
        long GetUInt32();
        long GetInt64();
        long GetUInt64();
        bool IsUInt64Null();
        void GetChars(char[] chars, int len);
    }
}
