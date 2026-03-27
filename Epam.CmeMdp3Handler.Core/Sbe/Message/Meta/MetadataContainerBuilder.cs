using System;
using Epam.CmeMdp3Handler.Sbe.Schema.Vo;

namespace Epam.CmeMdp3Handler.Sbe.Message.Meta
{
    /// <summary>
    /// Builder of Metadata of the given MDP Message Type.
    /// </summary>
    public static class MetadataContainerBuilder
    {
        /// <summary>Builds a fully populated metadata container for the given message type.</summary>
        /// <param name="messageType">The MDP message type definition</param>
        /// <returns>A populated <see cref="IMetadataContainer"/> for the message type</returns>
        public static IMetadataContainer Build(MdpMessageType messageType)
        {
            int schemaId = messageType.GetMessageType().Id;
            var container = Allocate(messageType.GetSchema(), schemaId,
                SbeConstants.HEADER_SIZE, FindMaxFieldId(messageType), messageType.GetMessageType().Group.Count);

            foreach (var fieldType in messageType.GetMessageType().Field)
                container.AddFieldType(new SbeFieldType(container, fieldType));

            foreach (var groupType in messageType.GetMessageType().Group)
            {
                var fieldContainer = Allocate(messageType.GetSchema(), schemaId, FindMaxFieldId(groupType));
                foreach (var fieldType in groupType.Field)
                    fieldContainer.AddFieldType(new SbeFieldType(fieldContainer, fieldType));
                container.AddGroupType(new SbeGroupType(fieldContainer, groupType,
                    FindDimensionType(messageType, groupType.DimensionType)));
            }
            return container;
        }

        /// <summary>Allocates a container for group-level metadata (without offset).</summary>
        private static IMetadataContainer Allocate(MessageSchema schema, int schemaId, int maxFieldId)
            => new ArrayMetadataContainer(schema, schemaId, maxFieldId);

        /// <summary>Allocates a container for message-level metadata (with offset and group count).</summary>
        private static IMetadataContainer Allocate(MessageSchema schema, int schemaId, int offset, int maxFieldId, int totalGroupNum)
            => new ArrayMetadataContainer(schema, schemaId, offset, maxFieldId, totalGroupNum);

        /// <summary>Finds the composite dimension type (e.g. groupSizeEncoding) for a group.</summary>
        private static CompositeDataType FindDimensionType(MdpMessageType messageType, string dimTypeName)
        {
            foreach (var cdt in messageType.GetSchema().TypesList[0].Composite)
                if (cdt.Name == dimTypeName) return cdt;
            throw new InvalidOperationException($"Dimension type '{dimTypeName}' not found");
        }

        /// <summary>Finds the maximum field ID across all fields in a message type.</summary>
        private static int FindMaxFieldId(MdpMessageType messageType)
        {
            int max = 0;
            foreach (var f in messageType.GetMessageType().Field)
                if (f.Id > max) max = f.Id;
            return max;
        }

        /// <summary>Finds the maximum field ID across all fields in a group type.</summary>
        private static int FindMaxFieldId(GroupType groupType)
        {
            int max = 0;
            foreach (var f in groupType.Field)
                if (f.Id > max) max = f.Id;
            return max;
        }
    }
}
