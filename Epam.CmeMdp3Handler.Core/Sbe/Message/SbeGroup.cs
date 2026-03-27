using System;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler.Sbe.Message
{
    public class SbeGroup : SbeGroupFieldSet, IMdpGroup, IMutableMdpGroup
    {
        private int _numInGroup;
        private int _groupBodyOffset;
        private int _entryNum;

        public static IMdpGroup Instance() => new SbeGroup();

        public void Reset(ISbeBuffer buffer, SbeGroupType sbeGroupType, int blockLength, int numInGroup, int groupBodyOffset)
        {
            _sbeBuffer = buffer;
            _entryNum = 0;
            _sbeGroupType = sbeGroupType;
            _blockLength = blockLength;
            _numInGroup = numInGroup;
            _groupBodyOffset = groupBodyOffset;
        }

        public int GetNumInGroup() => _numInGroup;
        public int GetEntryNum() => _entryNum;

        public bool HasNext() => _entryNum < _numInGroup;

        public void Next()
        {
            if (_entryNum <= _numInGroup)
            {
                _entryNum++;
                _entryOffset = _groupBodyOffset + (_entryNum - 1) * _blockLength;
            }
            else
            {
                throw new InvalidOperationException("Out of group size");
            }
        }

        public void GetEntry(IMdpGroupEntry groupEntry)
        {
            ((IMutableMdpGroupEntry)groupEntry).Reset(Buffer(), _sbeGroupType!, _entryOffset, _blockLength);
        }

        public void GetEntry(int entryNum, IMdpGroupEntry groupEntry)
        {
            if (entryNum > _numInGroup)
                throw new ArgumentException("Out of group size");
            int offset = _groupBodyOffset + ((entryNum - 1) * _blockLength);
            ((IMutableMdpGroupEntry)groupEntry).Reset(Buffer(), _sbeGroupType!, offset, _blockLength);
        }

        public override IFieldSet Copy()
        {
            var copy = (IMutableMdpGroup)Instance();
            copy.Reset(Buffer().Copy(), _sbeGroupType!, _blockLength, _numInGroup, _groupBodyOffset);
            return (IFieldSet)copy;
        }
    }
}
