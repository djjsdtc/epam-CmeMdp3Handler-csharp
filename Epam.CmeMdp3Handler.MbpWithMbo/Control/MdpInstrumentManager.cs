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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Registry of all subscribed securities and their per-instrument controllers.
    ///
    /// Java: com.epam.cme.mdp3.control.MdpInstrumentManager
    /// C# note: Java uses Koloboke IntObjMap (primitive-key map, no boxing) for performance.
    ///          C# uses Dictionary&lt;int, InstrumentController&gt;. Modern .NET JIT avoids
    ///          boxing for value-type keys in Dictionary, making this performant enough.
    /// </summary>
    public class MdpInstrumentManager : IInstrumentManager
    {
        private static readonly ILogger Log =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MdpInstrumentManager>();

        private readonly Dictionary<int, InstrumentController> _instruments = new();
        private readonly string _channelId;
        private readonly IList<IChannelListener> _listeners;

        public MdpInstrumentManager(string channelId, IList<IChannelListener> listeners)
        {
            _channelId = channelId;
            _listeners = listeners;
        }

        public InstrumentController? GetInstrumentController(int securityId)
        {
            _instruments.TryGetValue(securityId, out InstrumentController? controller);
            return controller;
        }

        public void RegisterSecurity(int securityId, string? secDesc)
        {
            if (_instruments.TryGetValue(securityId, out InstrumentController? instrumentController))
            {
                instrumentController.Enable();
            }
            else
            {
                _instruments[securityId] = new InstrumentController(_listeners, _channelId, securityId, secDesc);
            }
        }

        public void DiscontinueSecurity(int securityId)
        {
            if (_instruments.TryGetValue(securityId, out InstrumentController? instrumentController))
            {
                instrumentController.Disable();
            }
            else
            {
                Log.LogWarning("DiscontinueSecurity method was called but there is no security with id '{SecurityId}'", securityId);
            }
        }

        public void UpdateSecDesc(int securityId, string? secDesc)
        {
            if (_instruments.TryGetValue(securityId, out InstrumentController? instrumentController))
            {
                instrumentController.UpdateSecDesc(secDesc);
            }
            else
            {
                Log.LogDebug("UpdateSecDesc method was called but there is no security with id '{SecurityId}'", securityId);
            }
        }
    }
}
