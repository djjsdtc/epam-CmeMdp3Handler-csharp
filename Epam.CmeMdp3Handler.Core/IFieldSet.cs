using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler
{
    /// <summary>
    /// Interface to Fields in MDP Message or Group.
    /// </summary>
    public interface IFieldSet
    {
        /// <summary>Gets the schema ID of this field set.</summary>
        int GetSchemaId();

        /// <summary>Returns the underlying SBE buffer.</summary>
        ISbeBuffer Buffer();

        /// <summary>Returns true if a field with the given tag ID exists in the schema.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        bool HasField(int tagId);

        /// <summary>Returns true if the field value is null (optional field with null value).</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        bool IsNull(int tagId);

        /// <summary>Gets the char value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        char GetChar(int tagId);

        /// <summary>Gets the uint8 value of the field as a short.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        short GetUInt8(int tagId);

        /// <summary>Gets the int8 value of the field as a byte.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        byte GetInt8(int tagId);

        /// <summary>Gets the int16 value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        short GetInt16(int tagId);

        /// <summary>Gets the uint16 value of the field as an int.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        int GetUInt16(int tagId);

        /// <summary>Gets the int32 value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        int GetInt32(int tagId);

        /// <summary>Gets the uint32 value of the field as a long.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        long GetUInt32(int tagId);

        /// <summary>Gets the int64 value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        long GetInt64(int tagId);

        /// <summary>Gets the uint64 value of the field as a long.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        long GetUInt64(int tagId);

        /// <summary>Gets the decimal (mantissa/exponent) value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        /// <param name="doubleVal">Output holder for the decimal value</param>
        /// <returns>True if the field is a decimal type and was populated</returns>
        bool GetDouble(int tagId, SbeDouble doubleVal);

        /// <summary>Gets the string value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        /// <param name="stringVal">Output holder for the string value</param>
        /// <returns>True if the field is a string type and was populated</returns>
        bool GetString(int tagId, SbeString stringVal);

        /// <summary>Gets the MonthYear value of the field.</summary>
        /// <param name="tagId">FIX tag ID of the field</param>
        /// <param name="monthYearVal">Output holder for the month/year value</param>
        /// <returns>True if the field is a MonthYear type and was populated</returns>
        bool GetMonthYear(int tagId, SbeMonthYear monthYearVal);

        /// <summary>Gets a repeating group from this field set.</summary>
        /// <param name="tagId">FIX tag ID of the group</param>
        /// <param name="mdpGroup">Output holder for the group</param>
        /// <returns>True if the group was found and populated</returns>
        bool GetGroup(int tagId, IMdpGroup mdpGroup);

        /// <summary>Creates a deep copy of this field set.</summary>
        IFieldSet Copy();
    }
}
