using System;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeString
    {
        private readonly char[] _chars;
        private int _length;

        private SbeString(int capacity)
        {
            _chars = new char[capacity];
        }

        public static SbeString Allocate(int capacity) => new SbeString(capacity);

        public char[] GetChars() => _chars;

        public int GetLength() => _length;

        public void SetLength(int length) { _length = length; }

        public void Reset()
        {
            _length = 0;
        }

        public char GetCharAt(int index) => _chars[index];

        public string GetString()
        {
            // Trim trailing null/space chars
            int end = _length;
            while (end > 0 && (_chars[end - 1] == '\0' || _chars[end - 1] == ' '))
                end--;
            return new string(_chars, 0, end);
        }

        public override string ToString() => GetString();
    }
}
