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

using System;
using System.Collections.Generic;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Routes incoming packets to per-instrument controllers and dispatches events.
    ///
    /// Java: com.epam.cme.mdp3.control.ChannelControllerRouter
    /// C# note: Java uses agrona IntHashSet for tracking securityIds within a packet.
    ///          C# uses HashSet&lt;int&gt;.
    ///          Java Consumer&lt;MdpMessage&gt; for emptyBookConsumers -> Action&lt;IMdpMessage&gt;.
    /// </summary>
    public class ChannelControllerRouter : IMdpChannelController
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ChannelControllerRouter>();

        private readonly IInstrumentManager _instrumentManager;
        private readonly IMdpGroupEntry _mdEntry = SbeGroupEntry.Instance();
        private readonly IMdpGroup _noMdEntriesGroup = SbeGroup.Instance();
        private readonly MdpMessageTypes _mdpMessageTypes;
        private readonly IList<IChannelListener> _channelListeners;
        private readonly IInstrumentObserver _instrumentObserver;
        private readonly IList<Action<IMdpMessage>> _emptyBookConsumers;
        private readonly string _channelId;
        private readonly IList<int>? _mboIncrementMessageTemplateIds;
        private readonly IList<int>? _mboSnapshotMessageTemplateIds;

        // Reusable set for tracking security IDs within a single packet (like IntHashSet)
        private readonly HashSet<int> _securityIds = new();

        public ChannelControllerRouter(string channelId, IInstrumentManager instrumentManager,
            MdpMessageTypes mdpMessageTypes, IList<IChannelListener> channelListeners,
            IInstrumentObserver instrumentObserver, IList<Action<IMdpMessage>> emptyBookConsumers,
            IList<int>? mboIncrementMessageTemplateIds, IList<int>? mboSnapshotMessageTemplateIds)
        {
            _channelId = channelId;
            _instrumentManager = instrumentManager;
            _mdpMessageTypes = mdpMessageTypes;
            _channelListeners = channelListeners;
            _instrumentObserver = instrumentObserver;
            _emptyBookConsumers = emptyBookConsumers;
            _mboIncrementMessageTemplateIds = mboIncrementMessageTemplateIds;
            _mboSnapshotMessageTemplateIds = mboSnapshotMessageTemplateIds;
        }

        public IList<int> GetMboIncrementMessageTemplateIds() =>
            _mboIncrementMessageTemplateIds
            ?? new List<int> { IMdpChannelController.MboIncrementMessageTemplateId43, IMdpChannelController.MboIncrementMessageTemplateId47 };

        public IList<int> GetMboSnapshotMessageTemplateIds() =>
            _mboSnapshotMessageTemplateIds
            ?? new List<int> { IMdpChannelController.MboSnapshotMessageTemplateId44, IMdpChannelController.MboSnapshotMessageTemplateId53 };

        public void HandleSnapshotPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            foreach (IMdpMessage mdpMessage in mdpPacket)
            {
                ((IMdpChannelController)this).UpdateSemanticMsgType(_mdpMessageTypes, mdpMessage);
                int securityId = GetSecurityId(mdpMessage);
                InstrumentController? instrumentController = _instrumentManager.GetInstrumentController(securityId);
                if (instrumentController != null)
                {
                    if (((IMdpChannelController)this).IsMboSnapshot(mdpMessage))
                    {
                        instrumentController.HandleMBOSnapshotMDEntry(mdpMessage);
                    }
                    else
                    {
                        instrumentController.HandleMBPSnapshotMDEntry(mdpMessage);
                    }
                }
            }
        }

        public void HandleIncrementalPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            IMdpGroup mdpGroup = feedContext.GetMdpGroupObj();
            IMdpGroupEntry mdpGroupEntry = feedContext.GetMdpGroupEntryObj();
            long msgSeqNum = mdpPacket.GetMsgSeqNum();
            foreach (IMdpMessage mdpMessage in mdpPacket)
            {
                ((IMdpChannelController)this).UpdateSemanticMsgType(_mdpMessageTypes, mdpMessage);
                SemanticMsgType? semanticMsgType = mdpMessage.GetSemanticMsgType();
                switch (semanticMsgType)
                {
                    case SemanticMsgType.MarketDataIncrementalRefresh:
                        HandleIncrementalMessage(mdpMessage, mdpGroup, mdpGroupEntry, msgSeqNum);
                        break;
                    case SemanticMsgType.QuoteRequest:
                        HandleQuoteRequest(mdpMessage);
                        break;
                    case SemanticMsgType.SecurityStatus:
                        HandleSecurityStatus(mdpMessage);
                        break;
                    case SemanticMsgType.SecurityDefinition:
                        _instrumentObserver.OnMessage(feedContext, mdpMessage);
                        break;
                    default:
                        Logger.LogDebug("Message has been ignored due to its SemanticMsgType '{SemanticMsgType}'", semanticMsgType);
                        break;
                }
            }
        }

        public void PreClose() { }

        public void Close() { }

        protected void RouteMboEntry(int securityId, IMdpMessage mdpMessage, IMdpGroupEntry orderIdEntry, IMdpGroupEntry? mdEntry, long msgSeqNum)
        {
            InstrumentController? instrumentController = _instrumentManager.GetInstrumentController(securityId);
            instrumentController?.HandleMBOIncrementMDEntry(mdpMessage, orderIdEntry, mdEntry, msgSeqNum);
        }

        protected void RouteMbpEntry(int securityId, IMdpMessage mdpMessage, IMdpGroupEntry mdEntry, long msgSeqNum)
        {
            InstrumentController? instrumentController = _instrumentManager.GetInstrumentController(securityId);
            instrumentController?.HandleMBPIncrementMDEntry(mdpMessage, mdEntry, msgSeqNum);
        }

        protected void RouteIncrementalComplete(HashSet<int> securityIds, IMdpMessage mdpMessage, long msgSeqNum)
        {
            foreach (int securityId in securityIds)
            {
                InstrumentController? instrumentController = _instrumentManager.GetInstrumentController(securityId);
                instrumentController?.HandleIncrementalComplete(mdpMessage, msgSeqNum);
            }
        }

        private void HandleIncrementalMessage(IMdpMessage mdpMessage, IMdpGroup mdpGroup, IMdpGroupEntry mdpGroupEntry, long msgSeqNum)
        {
            if (((IMdpChannelController)this).IsIncrementalMessageSupported(mdpMessage))
            {
                _securityIds.Clear();
                if (((IMdpChannelController)this).IsIncrementOnlyForMbo(mdpMessage))
                {
                    mdpMessage.GetGroup(MdConstants.NO_MD_ENTRIES, mdpGroup);
                    while (mdpGroup.HasNext())
                    {
                        mdpGroup.Next();
                        mdpGroup.GetEntry(mdpGroupEntry);
                        int securityId = GetSecurityId(mdpGroupEntry);
                        _securityIds.Add(securityId);
                        RouteMboEntry(securityId, mdpMessage, mdpGroupEntry, null, msgSeqNum);
                    }
                }
                else
                {
                    if (mdpMessage.GetGroup(MdConstants.NO_MD_ENTRIES, _noMdEntriesGroup))
                    {
                        while (_noMdEntriesGroup.HasNext())
                        {
                            _noMdEntriesGroup.Next();
                            _noMdEntriesGroup.GetEntry(_mdEntry);
                            MDEntryType mdEntryType = MDEntryTypeExtensions.FromFIX(_mdEntry.GetChar(MdConstants.INCR_RFRSH_MD_ENTRY_TYPE));
                            if (mdEntryType == MDEntryType.EmptyBook)
                            {
                                foreach (Action<IMdpMessage> consumer in _emptyBookConsumers)
                                    consumer(mdpMessage);
                            }
                            else
                            {
                                int securityId = _mdEntry.GetInt32(MdConstants.SECURITY_ID);
                                _securityIds.Add(securityId);
                                RouteMbpEntry(securityId, mdpMessage, _mdEntry, msgSeqNum);
                            }
                        }
                    }

                    if (mdpMessage.GetGroup(MdConstants.NO_ORDER_ID_ENTRIES, mdpGroup) && IsOrderEntityContainsReference(mdpGroup, mdpGroupEntry))
                    {
                        while (mdpGroup.HasNext())
                        {
                            mdpMessage.GetGroup(MdConstants.NO_MD_ENTRIES, _noMdEntriesGroup);
                            mdpGroup.Next();
                            mdpGroup.GetEntry(mdpGroupEntry);
                            short entryNum = mdpGroupEntry.GetUInt8(MdConstants.REFERENCE_ID);
                            _noMdEntriesGroup.GetEntry(entryNum, _mdEntry);
                            int securityId = _mdEntry.GetInt32(MdConstants.SECURITY_ID);
                            _securityIds.Add(securityId);
                            RouteMboEntry(securityId, mdpMessage, mdpGroupEntry, _mdEntry, msgSeqNum);
                        }
                    }
                }
                RouteIncrementalComplete(_securityIds, mdpMessage, msgSeqNum);
            }
        }

        /// <summary>
        /// Returns true if it is not a TradeSummary order entity.
        /// </summary>
        private static bool IsOrderEntityContainsReference(IMdpGroup mdpGroup, IMdpGroupEntry mdpGroupEntry)
        {
            if (mdpGroup.HasNext())
            {
                mdpGroup.GetEntry(1, mdpGroupEntry);
                return mdpGroupEntry.HasField(MdConstants.REFERENCE_ID);
            }
            return false;
        }

        private void HandleQuoteRequest(IMdpMessage mdpMessage)
        {
            foreach (IChannelListener listener in _channelListeners)
                listener.OnRequestForQuote(_channelId, mdpMessage);
        }

        private void HandleSecurityStatus(IMdpMessage mdpMessage)
        {
            int securityId = GetSecurityId(mdpMessage);
            foreach (IChannelListener listener in _channelListeners)
                listener.OnSecurityStatus(_channelId, securityId, mdpMessage);
        }

        private static int GetSecurityId(IMdpMessage mdpMessage) =>
            mdpMessage.GetInt32(MdConstants.SECURITY_ID);

        private static int GetSecurityId(IMdpGroupEntry mdpGroupEntry) =>
            mdpGroupEntry.GetInt32(MdConstants.SECURITY_ID);
    }
}
