namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to MDP Group.
    /// </summary>
    public interface IMdpGroup : IFieldSet
    {
        /// <summary>Returns the total number of entries in this group.</summary>
        int GetNumInGroup();

        /// <summary>Returns the current entry number (1-based after calling <see cref="Next"/>).</summary>
        int GetEntryNum();

        /// <summary>Returns true if there are more entries to iterate.</summary>
        bool HasNext();

        /// <summary>Advances to the next entry. Must call before accessing entry fields.</summary>
        void Next();

        /// <summary>Populates the given group entry with the current entry's data.</summary>
        /// <param name="groupEntry">Entry holder to populate</param>
        void GetEntry(IMdpGroupEntry groupEntry);

        /// <summary>Populates the given group entry with data from the specified entry number.</summary>
        /// <param name="entryNum">1-based entry number</param>
        /// <param name="groupEntry">Entry holder to populate</param>
        void GetEntry(int entryNum, IMdpGroupEntry groupEntry);
    }
}
