namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeDouble
    {
        private long _mantissa;
        private byte _exponent;
        private bool _isNull;

        public static SbeDouble Instance() => new SbeDouble();

        public void Reset()
        {
            _mantissa = 0;
            _exponent = 0;
            _isNull = false;
        }

        public long GetMantissa() => _mantissa;
        public void SetMantissa(long mantissa) { _mantissa = mantissa; }

        public byte GetExponent() => _exponent;
        public void SetExponent(byte exponent) { _exponent = exponent; }

        public bool IsNull() => _isNull;
        public void SetNull(bool isNull) { _isNull = isNull; }

        public double AsDouble() => _mantissa * System.Math.Pow(10, _exponent);
    }
}
