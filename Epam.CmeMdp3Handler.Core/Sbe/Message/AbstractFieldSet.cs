using System;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public abstract class AbstractFieldSet : IFieldSet
    {
        public const string MATURITY_MONTH_YEAR = "MaturityMonthYear";

        protected ISbeBuffer? _sbeBuffer;

        public ISbeBuffer Buffer() => _sbeBuffer!;

        public virtual int GetSchemaId() => Metadata().GetSchemaId();

        public abstract IMetadataContainer Metadata();

        protected abstract void Seek(int tagId);
        protected abstract void Seek(SbeFieldType field);

        public bool HasField(int tagId) => Metadata().FindField(tagId) != null;

        public bool IsNull(int tagId)
        {
            var fieldType = Metadata().FindField(tagId)!;
            if (fieldType.IsOptional)
            {
                Seek(fieldType);
                if (fieldType.IsComposite)
                    return IsCompositeNull(fieldType);
                else
                    return IsPrimitiveNull(fieldType);
            }
            return false;
        }

        private bool IsCompositeNull(SbeFieldType fieldType)
        {
            if (fieldType.IsFloat)
            {
                if (fieldType.PrimitiveType == SbePrimitiveType.Int32)
                    return fieldType.Int32PresenceVal == Buffer().GetInt32();
                else if (fieldType.PrimitiveType == SbePrimitiveType.Int64)
                    return fieldType.Int64PresenceVal == Buffer().GetInt64();
            }
            return false;
        }

        private bool IsPrimitiveNull(SbeFieldType fieldType)
        {
            var pt = fieldType.PrimitiveType!;
            if (pt == SbePrimitiveType.Char)   return fieldType.CharPresenceVal?[0] == Buffer().GetChar();
            if (pt == SbePrimitiveType.Int8)   return fieldType.Int8PresenceVal == Buffer().GetInt8();
            if (pt == SbePrimitiveType.UInt8)  return fieldType.UInt8PresenceVal == Buffer().GetUInt8();
            if (pt == SbePrimitiveType.Int16)  return fieldType.UInt16PresenceVal == Buffer().GetInt16();
            if (pt == SbePrimitiveType.UInt16) return fieldType.UInt16PresenceVal == Buffer().GetUInt16();
            if (pt == SbePrimitiveType.Int32)  return fieldType.Int32PresenceVal == Buffer().GetInt32();
            if (pt == SbePrimitiveType.UInt32) return fieldType.UInt32PresenceVal == Buffer().GetUInt32();
            if (pt == SbePrimitiveType.Int64)  return fieldType.Int64PresenceVal == Buffer().GetInt64();
            if (pt == SbePrimitiveType.UInt64) return Buffer().IsUInt64Null();
            throw new InvalidOperationException($"Unknown primitive type {pt}");
        }

        public char GetChar(int tagId)
        {
            //handles only single char val now. Should possible generic case be also implemented?
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.CharPresenceVal![0];
            Seek(f); return Buffer().GetChar();
        }

        public short GetInt16(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.Int16PresenceVal;
            Seek(f); return Buffer().GetInt16();
        }

        public short GetUInt8(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.UInt8PresenceVal;
            Seek(f); return Buffer().GetUInt8();
        }

        public byte GetInt8(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.Int8PresenceVal;
            Seek(f); return Buffer().GetInt8();
        }

        public int GetUInt16(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.UInt16PresenceVal;
            Seek(f); return Buffer().GetUInt16();
        }

        public int GetInt32(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.Int32PresenceVal;
            Seek(f); return Buffer().GetInt32();
        }

        public long GetUInt32(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.UInt32PresenceVal;
            Seek(f); return Buffer().GetUInt32();
        }

        public long GetInt64(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.Int64PresenceVal;
            Seek(f); return Buffer().GetInt64();
        }

        public long GetUInt64(int tagId)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsConstant) return f.UInt64PresenceVal;
            Seek(f); return Buffer().GetUInt64();
        }

        public bool GetDouble(int tagId, SbeDouble doubleVal)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsFloat)
            {
                doubleVal.Reset();
                doubleVal.SetExponent(f.ExponentVal);
                Seek(f);
                if (f.PrimitiveType == SbePrimitiveType.Int32)
                    doubleVal.SetMantissa(Buffer().GetInt32());
                else if (f.PrimitiveType == SbePrimitiveType.Int64)
                    doubleVal.SetMantissa(Buffer().GetInt64());
                if (f.IsOptional)
                    doubleVal.SetNull(doubleVal.GetMantissa() == f.Int64PresenceVal);
                return true;
            }
            return false;
        }

        public bool GetMonthYear(int tagId, SbeMonthYear monthYearVal)
        {
            var f = Metadata().FindField(tagId)!;
            if (f.IsComposite && f.GetFieldType().Name == MATURITY_MONTH_YEAR)
            {
                monthYearVal.Reset();
                Seek(f);
                int valuePos = Buffer().Position();
                monthYearVal.SetYear(Buffer().GetUInt16());
                valuePos += SbePrimitiveType.UInt16.Size;
                Buffer().Position(valuePos);
                monthYearVal.SetMonth((short)Buffer().GetUInt8());
                valuePos += SbePrimitiveType.UInt8.Size;
                Buffer().Position(valuePos);
                monthYearVal.SetDay((short)Buffer().GetUInt8());
                valuePos += SbePrimitiveType.UInt8.Size;
                Buffer().Position(valuePos);
                monthYearVal.SetWeek((short)Buffer().GetUInt8());
                return true;
            }
            return false;
        }

        public bool GetString(int tagId, SbeString stringVal)
        {
            stringVal.Reset();
            var f = Metadata().FindField(tagId)!;
            if (f.IsString)
            {
                Seek(f);
                Buffer().GetChars(stringVal.GetChars(), f.Length);
                stringVal.SetLength(f.Length);
                return true;
            }
            return false;
        }

        public abstract bool GetGroup(int tagId, IMdpGroup mdpGroup); // optional action?

        public abstract IFieldSet Copy();
    }
}
