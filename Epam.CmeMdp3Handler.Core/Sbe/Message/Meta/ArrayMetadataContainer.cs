using System;
using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /// <summary>
    /// Array-based holder of MDP Message or Group metadata.
    /// </summary>
    public class ArrayMetadataContainer : IMetadataContainer
    {
        private readonly MessageSchema _schema;
        private readonly int _schemaId;
        private readonly int _offset;
        private readonly SbeFieldType?[] _fieldTypes;
        private SbeGroupType[]? _groupTypes;
        private int _groupCount;

        /// <summary>
        /// Creates a new container for root-level message metadata.
        /// </summary>
        /// <param name="schema">The message schema</param>
        /// <param name="schemaId">Schema ID of the message type</param>
        /// <param name="maxFieldId">Maximum field tag ID in this message type</param>
        public ArrayMetadataContainer(MessageSchema schema, int schemaId, int maxFieldId)
        {
            _schema = schema;
            _schemaId = schemaId;
            _fieldTypes = new SbeFieldType[maxFieldId + 1];
        }

        /// <summary>
        /// Creates a new container for group-level metadata with offset and group allocation.
        /// </summary>
        /// <param name="schema">The message schema</param>
        /// <param name="schemaId">Schema ID of the owning message type</param>
        /// <param name="offset">Byte offset of the field block within the message</param>
        /// <param name="maxFieldId">Maximum field tag ID in this group</param>
        /// <param name="totalGroupNum">Number of nested groups to pre-allocate</param>
        public ArrayMetadataContainer(MessageSchema schema, int schemaId, int offset, int maxFieldId, int totalGroupNum)
        {
            _schema = schema;
            _schemaId = schemaId;
            _offset = offset;
            _fieldTypes = new SbeFieldType[maxFieldId + 1];
            if (totalGroupNum > 0)
                _groupTypes = new SbeGroupType[totalGroupNum];
        }

        public MessageSchema GetSchema() => _schema;
        public int GetSchemaId() => _schemaId;
        public int Offset() => _offset;

        public void AddFieldType(SbeFieldType fieldType)
        {
            _fieldTypes[fieldType.GetFieldType().Id] = fieldType;
        }

        public void AddGroupType(SbeGroupType groupType)
        {
            if (_groupTypes == null)
                throw new InvalidOperationException("No group types allocated");
            if (_groupCount >= _groupTypes.Length)
                throw new InvalidOperationException("Incorrect pre-allocated number of Groups");
            _groupTypes[_groupCount++] = groupType;
        }

        public bool HasField(int fieldId) =>
            fieldId < _fieldTypes.Length && _fieldTypes[fieldId] != null;

        public bool HasGroup(int groupId)
        {
            if (_groupTypes == null) return false;
            foreach (var g in _groupTypes)
                if (g != null && g.GetGroupType().Id == groupId) return true;
            return false;
        }

        public SbeFieldType? FindField(int fieldId) =>
            fieldId < _fieldTypes.Length ? _fieldTypes[fieldId] : null;

        public SbeGroupType? FindGroup(int groupId)
        {
            if (_groupTypes == null) return null;
            foreach (var g in _groupTypes)
                if (g != null && g.GetGroupType().Id == groupId) return g;
            return null;
        }

        public SbeGroupType[]? AllGroups() => _groupTypes;
        public SbeFieldType?[]? AllFields() => _fieldTypes;

        SbeFieldType[]? IMetadataContainer.AllFields()
        {
            // Return the non-nullable array cast (nullable elements but non-null array)
            return (SbeFieldType[]?)(object?)_fieldTypes;
        }

        public EncodedDataType? GetDataType(string typeName)
        {
            foreach (var types in _schema.TypesList)
                foreach (var dt in types.Type)
                    if (dt.Name == typeName) return dt;
            return null;
        }
    }
}
