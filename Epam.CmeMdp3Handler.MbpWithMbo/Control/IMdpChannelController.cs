/*
 * Copyright 2004-2016 EPAM Systems
 * This file is part of Java Market Data Handler for CME Market Data (MDP 3.0).
 * Java Market Data Handler for CME Market Data (MDP 3.0) is free software: you can redistribute it and/or modify it under the terms of the
 * GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Java Market Data Handler for CME Market Data (MDP 3.0) is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Java Market Data Handler for CME Market Data (MDP 3.0).
 * If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;
using Epam.CmeMdp3Handler.Sbe.Schema;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Extension of IChannelController that adds MBO template ID constants and helper methods.
    ///
    /// Java: com.epam.cme.mdp3.control.MdpChannelController
    /// C# note: Java uses interface default methods; C# provides these as virtual methods
    ///          in abstract/concrete implementations or as extension helpers here using
    ///          default interface members (supported in C# 8+ / .NET 8).
    /// </summary>
    public interface IMdpChannelController : IChannelController
    {
        /// <summary>MBO incremental refresh template ID (template 43)</summary>
        const int MboIncrementMessageTemplateId43 = 43;
        /// <summary>MBO incremental refresh template ID (template 47)</summary>
        const int MboIncrementMessageTemplateId47 = 47;
        /// <summary>MBO snapshot full refresh template ID (template 44)</summary>
        const int MboSnapshotMessageTemplateId44 = 44;
        /// <summary>MBO snapshot full refresh template ID (template 53)</summary>
        const int MboSnapshotMessageTemplateId53 = 53;

        /// <summary>Returns the list of MBO incremental message template IDs.</summary>
        IList<int> GetMboIncrementMessageTemplateIds();

        /// <summary>Returns the list of MBO snapshot message template IDs.</summary>
        IList<int> GetMboSnapshotMessageTemplateIds();

        /// <summary>Updates the message type metadata on the given message.</summary>
        void UpdateSemanticMsgType(MdpMessageTypes mdpMessageTypes, IMdpMessage mdpMessage)
        {
            int schemaId = mdpMessage.GetSchemaId();
            MdpMessageType messageType = mdpMessageTypes.GetMessageType(schemaId);
            mdpMessage.SetMessageType(messageType);
        }

        /// <summary>Returns true if the message is an incremental refresh.</summary>
        bool IsIncrementalMessageSupported(IMdpMessage mdpMessage)
        {
            SemanticMsgType? semanticMsgType = mdpMessage.GetSemanticMsgType();
            return semanticMsgType == SemanticMsgType.MarketDataIncrementalRefresh;
        }

        /// <summary>Returns true if the message is an MBO-only incremental refresh.</summary>
        bool IsIncrementOnlyForMbo(IMdpMessage mdpMessage)
        {
            SemanticMsgType? semanticMsgType = mdpMessage.GetSemanticMsgType();
            int schemaId = mdpMessage.GetSchemaId();
            return semanticMsgType == SemanticMsgType.MarketDataIncrementalRefresh
                   && GetMboIncrementMessageTemplateIds().Contains(schemaId);
        }

        /// <summary>Returns true if the message is an MBO snapshot.</summary>
        bool IsMboSnapshot(IMdpMessage mdpMessage)
        {
            int schemaId = mdpMessage.GetSchemaId();
            return GetMboSnapshotMessageTemplateIds().Contains(schemaId);
        }
    }
}
