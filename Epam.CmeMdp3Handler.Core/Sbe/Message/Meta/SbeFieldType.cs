using System;
using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /// <summary>
    /// Metadata of MDP Field type.
    /// </summary>
    public class SbeFieldType
    {
        private const string PRESENCE_OPTIONAL = "optional";
        private const string PRESENCE_CONSTANT = "constant";
        private const string FLOAT_MANTISSA    = "mantissa";
        private const string FLOAT_EXPONENT    = "exponent";

        private readonly FieldType _fieldType;
        private readonly IMetadataContainer _metadataContainer;

        public SbePrimitiveType? PrimitiveType { get; private set; }
        public bool IsComposite { get; private set; }
        public bool IsConstant  { get; private set; }
        public bool IsOptional  { get; private set; }
        public bool IsString    { get; private set; }
        public int  Length      { get; private set; }
        public bool IsFloat     { get; private set; }
        public byte ExponentVal { get; private set; }

        public string? CharPresenceVal  { get; private set; }
        public byte    Int8PresenceVal  { get; private set; }
        public short   UInt8PresenceVal { get; private set; }
        public short   Int16PresenceVal { get; private set; }
        public int     UInt16PresenceVal{ get; private set; }
        public int     Int32PresenceVal { get; private set; }
        public long    UInt32PresenceVal{ get; private set; }
        public long    Int64PresenceVal { get; private set; }
        public long    UInt64PresenceVal{ get; private set; }

        public FieldType GetFieldType() => _fieldType;

        /// <summary>
        /// Creates and initializes field type metadata by resolving the field's type from the schema.
        /// </summary>
        /// <param name="metadataContainer">The parent metadata container</param>
        /// <param name="fieldType">The field type definition from the schema</param>
        public SbeFieldType(IMetadataContainer metadataContainer, FieldType fieldType)
        {
            _metadataContainer = metadataContainer;
            _fieldType = fieldType;
            Init();
        }

        private void Init()
        {
            var types = _metadataContainer.GetSchema().TypesList[0];
            foreach (var edt in types.Type)
            {
                if (edt.Name == _fieldType.Type)
                {
                    InitFromEncodedDataType(edt);
                    return;
                }
            }
            foreach (var cdt in types.Composite)
            {
                if (cdt.Name == _fieldType.Type)
                {
                    InitFromCompositeDataType(cdt);
                    return;
                }
            }
            foreach (var et in types.Enum)
            {
                if (et.Name == _fieldType.Type)
                {
                    InitFromEnumType(et);
                    return;
                }
            }
            foreach (var st in types.Set)
            {
                if (st.Name == _fieldType.Type)
                {
                    InitFromSetType(st);
                    return;
                }
            }
        }

        /// <summary>Initializes field type from an encoded (primitive) data type definition.</summary>
        private void InitFromEncodedDataType(EncodedDataType edt)
        {
            PrimitiveType = SbePrimitiveType.FromString(edt.PrimitiveType);
            if (edt.Length != null && PrimitiveType == SbePrimitiveType.Char)
            {
                Length = edt.Length.Value;
                IsString = true;
            }
            if (edt.Presence != null)
            {
                if (edt.Presence == PRESENCE_OPTIONAL && edt.NullValue != null)
                {
                    IsOptional = true;
                    SetPresenceValue(edt.NullValue);
                }
                else if (edt.Presence == PRESENCE_CONSTANT && edt.Value != null)
                {
                    IsConstant = true;
                    SetPresenceValue(edt.Value);
                }
            }
        }

        /// <summary>Initializes field type from a composite data type definition (e.g. PRICE, MaturityMonthYear).</summary>
        private void InitFromCompositeDataType(CompositeDataType cdt)
        {
            IsComposite = true;
            if (cdt.Type.Count == 2)
            {
                var type1 = cdt.Type[0];
                var type2 = cdt.Type[1];
                if (type1.Name == FLOAT_MANTISSA && type2.Name == FLOAT_EXPONENT)
                {
                    IsFloat = true;
                    PrimitiveType = SbePrimitiveType.FromString(type1.PrimitiveType);
                    if (type1.Presence == PRESENCE_OPTIONAL && type1.NullValue != null)
                    {
                        IsOptional = true;
                        Int64PresenceVal = long.Parse(type1.NullValue);
                    }
                    ExponentVal = byte.Parse(type2.Value ?? "0");
                }
            }
        }

        /// <summary>Initializes field type from an enum type definition.</summary>
        private void InitFromEnumType(EnumType et)
        {
            var dataType = _metadataContainer.GetDataType(et.EncodingType);
            if (dataType != null)
                PrimitiveType = SbePrimitiveType.FromString(dataType.PrimitiveType);
        }

        /// <summary>Initializes field type from a set (bitset) type definition.</summary>
        private void InitFromSetType(SetType st)
        {
            var dataType = _metadataContainer.GetDataType(st.EncodingType);
            if (dataType != null)
                PrimitiveType = SbePrimitiveType.FromString(dataType.PrimitiveType);
        }

        /// <summary>Parses and stores the presence value (null or constant) for the given primitive type.</summary>
        private void SetPresenceValue(string val)
        {
            switch (PrimitiveType!.Name)
            {
                case "char":   CharPresenceVal   = val; break;
                case "int8":   Int8PresenceVal   = byte.Parse(val); break;
                case "uint8":  UInt8PresenceVal  = short.Parse(val); break;
                case "int16":  Int16PresenceVal  = short.Parse(val); break;
                case "uint16": UInt16PresenceVal = int.Parse(val); break;
                case "int32":  Int32PresenceVal  = int.Parse(val); break;
                case "uint32": UInt32PresenceVal = long.Parse(val); break;
                case "int64":  Int64PresenceVal  = long.Parse(val); break;
                case "uint64": UInt64PresenceVal = unchecked((long)ulong.Parse(val)); break;
                default: throw new InvalidOperationException($"Unknown type {PrimitiveType.Name}");
            }
        }

        public void Seek(ISbeBuffer buffer)
        {
            buffer.Position(_metadataContainer.Offset() + (int)_fieldType.Offset!.Value);
        }
    }
}
