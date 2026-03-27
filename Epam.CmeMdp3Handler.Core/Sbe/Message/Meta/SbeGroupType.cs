using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    public class SbeGroupType
    {
        private readonly IMetadataContainer _fieldContainer;
        private readonly GroupType _groupType;
        private readonly CompositeDataType _dimensionType;

        public int NumInGroupOffset    { get; private set; }
        public int DimensionBlockLength{ get; private set; }

        public SbeGroupType(IMetadataContainer metadataContainer, GroupType groupType, CompositeDataType dimensionType)
        {
            _fieldContainer = metadataContainer;
            _groupType = groupType;
            _dimensionType = dimensionType;
            CalcDimensionBlockFields();
        }

        public GroupType GetGroupType() => _groupType;

        public IMetadataContainer GetMetadataContainer() => _fieldContainer;

        private void CalcDimensionBlockFields()
        {
            var blockLengthType = _dimensionType.Type[0];
            var numInGroupType  = _dimensionType.Type[1];
            int offset = numInGroupType.Offset;
            if (offset > 0)
            {
                NumInGroupOffset = offset;
            }
            else
            {
                NumInGroupOffset = SbePrimitiveType.FromString(blockLengthType.PrimitiveType).Size;
            }
            DimensionBlockLength = NumInGroupOffset + SbePrimitiveType.FromString(numInGroupType.PrimitiveType).Size;
        }
    }
}
