using Epam.CmeMdp3Handler.Sbe.Message.Meta;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to MDP Group Entry.
    /// </summary>
    public interface IMdpGroupEntry : IFieldSet
    {
        /// <summary>Returns the absolute byte offset of this entry in the buffer.</summary>
        int GetAbsoluteEntryOffset();

        /// <summary>Returns the SBE group type metadata for this entry.</summary>
        SbeGroupType GetSbeGroupType();

        /// <summary>Returns the block length of this group entry.</summary>
        int GetBlockLength();
    }
}
