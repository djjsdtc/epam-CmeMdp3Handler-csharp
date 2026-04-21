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

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Dispatches market-data events to all registered listeners for a single security.
    ///
    /// Java: com.epam.cme.mdp3.control.InstrumentController
    /// </summary>
    public class InstrumentController
    {
        private readonly IList<IChannelListener> _listeners;
        private readonly string _channelId;
        private readonly int _securityId;
        private string? _secDesc;
        private volatile bool _enable = true;

        public InstrumentController(IList<IChannelListener> listeners, string channelId, int securityId, string? secDesc)
        {
            _listeners = listeners;
            _channelId = channelId;
            _securityId = securityId;
            _secDesc = secDesc;
        }

        public void HandleMBOIncrementMDEntry(IMdpMessage mdpMessage, IMdpGroupEntry orderIDEntry, IMdpGroupEntry? mdEntry, long msgSeqNum)
        {
            if (_enable)
            {
                foreach (IChannelListener channelListener in _listeners)
                {
                    channelListener.OnIncrementalMBORefresh(mdpMessage, _channelId, _securityId, _secDesc, msgSeqNum, orderIDEntry, mdEntry);
                }
            }
        }

        public void HandleMBPIncrementMDEntry(IMdpMessage mdpMessage, IMdpGroupEntry mdEntry, long msgSeqNum)
        {
            if (_enable)
            {
                foreach (IChannelListener channelListener in _listeners)
                {
                    channelListener.OnIncrementalMBPRefresh(mdpMessage, _channelId, _securityId, _secDesc, msgSeqNum, mdEntry);
                }
            }
        }

        public void HandleIncrementalComplete(IMdpMessage mdpMessage, long msgSeqNum)
        {
            if (_enable)
            {
                foreach (IChannelListener channelListener in _listeners)
                {
                    channelListener.OnIncrementalComplete(mdpMessage, _channelId, _securityId, _secDesc, msgSeqNum);
                }
            }
        }

        public void HandleMBOSnapshotMDEntry(IMdpMessage mdpMessage)
        {
            if (_enable)
            {
                foreach (IChannelListener channelListener in _listeners)
                {
                    channelListener.OnSnapshotMBOFullRefresh(_channelId, _secDesc, mdpMessage);
                }
            }
        }

        public void HandleMBPSnapshotMDEntry(IMdpMessage mdpMessage)
        {
            if (_enable)
            {
                foreach (IChannelListener channelListener in _listeners)
                {
                    channelListener.OnSnapshotMBPFullRefresh(_channelId, _secDesc, mdpMessage);
                }
            }
        }

        public void Enable() { _enable = true; }

        public void Disable() { _enable = false; }

        public void UpdateSecDesc(string? secDesc) { _secDesc = secDesc; }
    }
}
