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
using System.IO;
using System.Net;
using System.Net.Sockets;
using Epam.CmeMdp3Handler.Core.Cfg;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Core.Channel.Tcp
{
    /// <summary>
    /// TCP channel implementation for the CME replay feed.
    ///
    /// Java: com.epam.cme.mdp3.core.channel.tcp.MdpTCPChannel
    /// C# note: Java uses java.nio.channels.SocketChannel (NIO). C# uses System.Net.Sockets.TcpClient.
    /// </summary>
    public class MdpTcpChannel : ITcpChannel
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MdpTcpChannel>();

        private readonly ConnectionCfg _cfg;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly MdpFeedContext _feedContext;

        public MdpTcpChannel(ConnectionCfg cfg)
        {
            _cfg = cfg;
            _feedContext = new MdpFeedContext(cfg);
        }

        public bool Connect()
        {
            foreach (string hostIp in _cfg.HostIPs)
            {
                try
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.Connect(new IPEndPoint(IPAddress.Parse(hostIp), _cfg.Port));
                    _stream = _tcpClient.GetStream();
                    Logger.LogDebug("Connected to {Host}:{Port}", hostIp, _cfg.Port);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to connect to {Host}:{Port}", hostIp, _cfg.Port);
                }
            }
            return false;
        }

        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{Message}", e.Message);
            }
        }

        public void Send(byte[] data, int length)
        {
            if (_stream == null) throw new IOException("Not connected");
            _stream.Write(data, 0, length);
        }

        public int Receive(byte[] buffer, int length)
        {
            if (_stream == null) throw new IOException("Not connected");
            int bytesRead = _stream.Read(buffer, 0, length);
            if (bytesRead < 0)
                throw new EndOfStreamException($"Length of last received bytes is less than zero '{bytesRead}'");
            return bytesRead;
        }

        public MdpFeedContext GetFeedContext() => _feedContext;
    }
}
