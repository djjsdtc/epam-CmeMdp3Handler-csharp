using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to modify Group Entry. Usually not available via the most public API.
    /// </summary>
    public interface IMutableMdpGroupEntry : IMdpGroupEntry
    {
        void Reset(ISbeBuffer buffer, SbeGroupType sbeGroupType, int entryOffset, int blockLength);
    }
}
