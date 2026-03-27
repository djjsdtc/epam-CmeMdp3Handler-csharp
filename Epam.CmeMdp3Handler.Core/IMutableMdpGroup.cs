using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to modify Group. Usually not available via the most public API.
    /// </summary>
    public interface IMutableMdpGroup : IMdpGroup
    {
        void Reset(ISbeBuffer buffer, SbeGroupType sbeGroupType, int blockLength, int numInGroup, int groupBodyOffset);
    }
}
