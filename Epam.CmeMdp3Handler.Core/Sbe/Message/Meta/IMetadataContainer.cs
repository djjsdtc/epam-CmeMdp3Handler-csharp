using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /// <summary>
    /// Interface to MDP Metadata Container.
    /// </summary>
    public interface IMetadataContainer
    {
        /// <summary>Returns the message schema this container belongs to.</summary>
        MessageSchema GetSchema();

        /// <summary>Returns the schema ID of this metadata container.</summary>
        int GetSchemaId();

        /// <summary>Returns the byte offset of the field block within the enclosing message or group.</summary>
        int Offset();

        /// <summary>Returns true if a field with the given ID is present in this container.</summary>
        /// <param name="fieldId">Field tag ID</param>
        bool HasField(int fieldId);

        /// <summary>Returns true if a group with the given ID is present in this container.</summary>
        /// <param name="groupId">Group tag ID</param>
        bool HasGroup(int groupId);

        /// <summary>Finds and returns the field type metadata for the given field ID, or null if not found.</summary>
        /// <param name="fieldId">Field tag ID</param>
        SbeFieldType? FindField(int fieldId);

        /// <summary>Finds and returns the group type metadata for the given group ID, or null if not found.</summary>
        /// <param name="groupId">Group tag ID</param>
        SbeGroupType? FindGroup(int groupId);

        /// <summary>Returns all group type metadata in this container.</summary>
        SbeGroupType[]? AllGroups();

        /// <summary>Returns all field type metadata in this container.</summary>
        SbeFieldType[]? AllFields();

        /// <summary>Adds a field type to this container.</summary>
        /// <param name="fieldType">The field type metadata to add</param>
        void AddFieldType(SbeFieldType fieldType);

        /// <summary>Adds a group type to this container.</summary>
        /// <param name="groupType">The group type metadata to add</param>
        void AddGroupType(SbeGroupType groupType);

        /// <summary>Returns the encoded data type definition for the given type name from the schema.</summary>
        /// <param name="typeName">Type name as defined in the SBE schema XML</param>
        EncodedDataType? GetDataType(string typeName);
    }
}
