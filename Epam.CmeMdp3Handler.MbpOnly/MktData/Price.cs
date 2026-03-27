using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.MktData
{
    public class Price
    {
        private const byte Exponent = unchecked((byte)-7); // treated as signed -7 in math
        private long _mantissa;

        public long GetMantissa() => _mantissa;

        public void SetMantissa(long mantissa) => _mantissa = mantissa;

        public bool IsNull() => _mantissa == SbePrimitiveType.Int64.NullValue;

        public void SetNull() => _mantissa = SbePrimitiveType.Int64.NullValue;

        public double AsDouble() => _mantissa * 1e-7;

        public override bool Equals(object? obj)
        {
            if (obj is Price other) return _mantissa == other._mantissa;
            return false;
        }

        public override int GetHashCode() => _mantissa.GetHashCode();
    }
}
