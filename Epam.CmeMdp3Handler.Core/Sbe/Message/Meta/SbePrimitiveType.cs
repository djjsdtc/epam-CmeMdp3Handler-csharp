using System;
using System.Collections.Generic;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /* Primitive types provide basic SBE properties */
    public class SbePrimitiveType
    {
        /* @param name of primitive type in XML schema
         * @param size in bytes
         * @param isSigned whether the type is signed
         * @param minValue minimum representable value
         * @param maxValue maximum representable value
         * @param nullValue sentinel value indicating null */
        public static readonly SbePrimitiveType Char   = new("char",   1, true,  -127L,       127L,        127L);
        public static readonly SbePrimitiveType Int8   = new("int8",   1, true,  -127L,       127L,        127L);
        public static readonly SbePrimitiveType UInt8  = new("uint8",  1, false, 0L,          0xFEL,       255L);
        public static readonly SbePrimitiveType Int16  = new("int16",  2, true,  -32767L,     32767L,      32767L);
        public static readonly SbePrimitiveType UInt16 = new("uint16", 2, false, 0L,          0xFFFEL,     0xFFFEL);
        public static readonly SbePrimitiveType Int32  = new("int32",  4, true,  int.MinValue + 1L, int.MaxValue, int.MaxValue);
        public static readonly SbePrimitiveType UInt32 = new("uint32", 4, false, 0L,          0xFFFFFFFEL, 4294967295L);
        public static readonly SbePrimitiveType Int64  = new("int64",  8, true,  long.MinValue + 1L, long.MaxValue, long.MaxValue);
        public static readonly SbePrimitiveType UInt64 = new("uint64", 8, false, 0L,          long.MaxValue, long.MaxValue);

        /* name -> PrimitiveType map */
        /* Multitone object provider — one instance per primitive type name */
        /* Build name -> PrimitiveType map */
        private static readonly Dictionary<string, SbePrimitiveType> _map = new()
        {
            ["char"]   = Char,
            ["int8"]   = Int8,
            ["uint8"]  = UInt8,
            ["int16"]  = Int16,
            ["uint16"] = UInt16,
            ["int32"]  = Int32,
            ["uint32"] = UInt32,
            ["int64"]  = Int64,
            ["uint64"] = UInt64,
        };

        public string Name      { get; }
        public int    Size      { get; }
        public bool   IsSigned  { get; }
        public long   MinValue  { get; }
        public long   MaxValue  { get; }
        public long   NullValue { get; }

        private SbePrimitiveType(string name, int size, bool isSigned, long minValue, long maxValue, long nullValue)
        {
            Name      = name;
            Size      = size;
            IsSigned  = isSigned;
            MinValue  = minValue;
            MaxValue  = maxValue;
            NullValue = nullValue;
        }

        public static SbePrimitiveType FromString(string name)
        {
            if (_map.TryGetValue(name, out var type)) return type;
            throw new ArgumentException($"Unknown SBE primitive type: {name}");
        }

        public bool IsNull(long value) => value == NullValue;

        public override string ToString() => Name;
    }
}
