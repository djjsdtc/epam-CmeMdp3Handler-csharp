using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeGroupEntry : SbeGroupFieldSet, IMdpGroupEntry, IMutableMdpGroupEntry
    {
        public static IMdpGroupEntry Instance() => new SbeGroupEntry();

        public void Reset(ISbeBuffer buffer, SbeGroupType sbeGroupType, int entryOffset, int blockLength)
        {
            _sbeBuffer = buffer;
            _sbeGroupType = sbeGroupType;
            _entryOffset = entryOffset;
            _blockLength = blockLength;
        }

        // IMdpGroupEntry explicit implementations
        int IMdpGroupEntry.GetAbsoluteEntryOffset() => GetAbsoluteEntryOffset();
        SbeGroupType IMdpGroupEntry.GetSbeGroupType() => GetSbeGroupType()!;
        int IMdpGroupEntry.GetBlockLength() => GetBlockLength();

        public override IFieldSet Copy()
        {
            var copy = (IMutableMdpGroupEntry)Instance();
            copy.Reset(Buffer().Copy(), _sbeGroupType!, _entryOffset, _blockLength);
            return (IFieldSet)copy;
        }
    }
}
