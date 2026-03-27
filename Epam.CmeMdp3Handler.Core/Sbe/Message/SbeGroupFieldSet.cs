using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public abstract class SbeGroupFieldSet : AbstractFieldSet
    {
        protected SbeGroupType? _sbeGroupType;
        protected int _entryOffset;
        protected int _blockLength;

        public SbeGroupType? GetSbeGroupType() => _sbeGroupType;

        public int GetAbsoluteEntryOffset() => Buffer().Offset() + _entryOffset;

        public int GetBlockLength() => _blockLength;

        public override IMetadataContainer Metadata() => _sbeGroupType!.GetMetadataContainer();

        protected override void Seek(SbeFieldType field)
        {
            int offset;
            if (field.GetFieldType().Offset != null)
                offset = _entryOffset + (int)field.GetFieldType().Offset!.Value;
            else
                offset = _entryOffset + _blockLength - field.PrimitiveType!.Size;
            Buffer().Position(offset);
        }

        protected override void Seek(int tagId) => Seek(Metadata().FindField(tagId)!);

        public override bool GetGroup(int tagId, IMdpGroup mdpGroup)
            => throw new System.NotSupportedException();
    }
}
