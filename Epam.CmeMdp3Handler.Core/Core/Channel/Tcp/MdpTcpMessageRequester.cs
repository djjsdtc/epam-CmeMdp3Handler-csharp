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
using System.Text;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Core.Channel.Tcp
{
    /// <summary>
    /// Implements TCP-based message gap recovery using the CME TCP replay feed.
    ///
    /// Java: com.epam.cme.mdp3.core.channel.tcp.MdpTCPMessageRequester
    /// C# note: Java uses java.nio.ByteBuffer (direct/heap) and NIO SocketChannel.
    ///          C# uses byte[] and TcpClient via ITcpChannel abstraction.
    ///          Java synchronized method -> C# lock statement.
    /// </summary>
    public class MdpTcpMessageRequester : ITcpMessageRequester
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MdpTcpMessageRequester>();

        public const string DefaultUsername = "CME";
        public const string DefaultPassword = "CME";
        public const int NumberOfSizeBytes = 2;

        private const int RequestTimeoutInMsec = 4000;
        private const int LogonTemplateId = 15;
        private const int LogoutTemplateId = 16;
        private const int LogoutTextFieldId = 58;

        private readonly IEnumerable<ICoreChannelListener> _coreChannelListeners;
        private readonly MdpMessageTypes _mdpMessageTypes;
        private readonly ITcpChannel _tcpChannel;
        private readonly byte[] _logon;
        private readonly byte[] _logout;
        private readonly MdpPacket _mdpPacket = MdpPacket.Instance();
        private readonly byte[] _workBuffer = new byte[SbeConstants.MDP_PACKET_MAX_SIZE];
        private readonly SbeString _sbeString = SbeString.Allocate(500);
        private volatile int _requestCounter;
        private readonly string _channelId;

        private readonly object _lock = new object();

        public MdpTcpMessageRequester(string channelId, IEnumerable<ICoreChannelListener> coreChannelListeners,
            MdpMessageTypes mdpMessageTypes, ITcpChannel tcpChannel, string username, string password)
        {
            _channelId = channelId;
            _coreChannelListeners = coreChannelListeners;
            _mdpMessageTypes = mdpMessageTypes;
            _tcpChannel = tcpChannel;
            _logon = Encoding.ASCII.GetBytes(PrepareLogonMessage(username, password));
            _logout = Encoding.ASCII.GetBytes(PrepareLogoutMessage());
        }

        public bool AskForLostMessages(long beginSeqNo, long endSeqNo, ITcpPacketListener tcpPacketListener)
        {
            lock (_lock)
            {
                byte[] request = Encoding.ASCII.GetBytes(PrepareRequestMessage(_channelId, beginSeqNo, endSeqNo, ++_requestCounter));
                bool connected = _tcpChannel.Connect();
                if (connected)
                {
                    try
                    {
                        OnFeedStarted();
                        SendPacket(_logon);
                        MdpPacket? receivedPacket = ReceivePacket();
                        if (receivedPacket != null && IsPacketWithLogon(receivedPacket))
                        {
                            _tcpChannel.Send(request, request.Length);
                            long startRequestTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            long packetSeqNum;
                            do
                            {
                                if ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startRequestTime) > RequestTimeoutInMsec)
                                {
                                    Logger.LogWarning("Request has not been processed for {Timeout} msec", RequestTimeoutInMsec);
                                    return false;
                                }
                                receivedPacket = ReceivePacket();
                                if (receivedPacket == null) return false;
                                packetSeqNum = receivedPacket.GetMsgSeqNum();
                                if (packetSeqNum >= beginSeqNo && packetSeqNum <= endSeqNo)
                                {
                                    tcpPacketListener.OnPacket(_tcpChannel.GetFeedContext(), receivedPacket);
                                }
                            } while (packetSeqNum < endSeqNo);

                            receivedPacket = ReceivePacket();
                            return receivedPacket != null && IsPacketWithLogout(receivedPacket);
                        }
                        else if (receivedPacket != null)
                        {
                            var firstMessageEnum = receivedPacket.GetEnumerator();
                            if (firstMessageEnum.MoveNext())
                            {
                                IMdpMessage firstMessage = firstMessageEnum.Current;
                                int schemaId = firstMessage.GetSchemaId();
                                firstMessage.SetMessageType(_mdpMessageTypes.GetMessageType(schemaId));
                                Logger.LogWarning("The message {Message} has been received instead of logon", firstMessage);
                                if (schemaId == LogoutTemplateId)
                                {
                                    firstMessage.GetString(LogoutTextFieldId, _sbeString);
                                    Logger.LogWarning("Logout has been received with reason '{Reason}'", _sbeString.GetString());
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Last packet [{Packet}] - {Message}", _mdpPacket, e.Message);
                    }
                    finally
                    {
                        try { SendPacket(_logout); }
                        catch (Exception e) { Logger.LogError(e, "Error has occurred during logout"); }
                        _tcpChannel.Disconnect();
                        OnFeedStopped();
                    }
                }
                return false;
            }
        }

        private void OnFeedStarted()
        {
            FeedType feedType = _tcpChannel.GetFeedContext().FeedType;
            Feed feed = _tcpChannel.GetFeedContext().Feed;
            foreach (var listener in _coreChannelListeners)
                listener.OnFeedStarted(_channelId, feedType, feed);
        }

        private void OnFeedStopped()
        {
            FeedType feedType = _tcpChannel.GetFeedContext().FeedType;
            Feed feed = _tcpChannel.GetFeedContext().Feed;
            foreach (var listener in _coreChannelListeners)
                listener.OnFeedStopped(_channelId, feedType, feed);
        }

        private void SendPacket(byte[] data)
        {
            _tcpChannel.Send(data, data.Length);
        }

        private MdpPacket? ReceivePacket()
        {
            // Read 2 size bytes
            int read = _tcpChannel.Receive(_workBuffer, NumberOfSizeBytes);
            if (read < NumberOfSizeBytes) return null;
            int nextPacketSize = (_workBuffer[0] & 0xFF) | ((_workBuffer[1] & 0xFF) << 8); // little-endian UInt16
            read = _tcpChannel.Receive(_workBuffer, nextPacketSize);
            if (read < nextPacketSize) return null;
            _mdpPacket.Buffer().WrapForParse(_workBuffer, nextPacketSize);
            _mdpPacket.Length(nextPacketSize);
            Logger.LogTrace("Packet [{Packet}] has been read", _mdpPacket);
            return _mdpPacket;
        }

        private bool IsPacketWithLogon(MdpPacket mdpPacket)
        {
            if (mdpPacket.GetMsgSeqNum() == 1)
            {
                var it = mdpPacket.GetEnumerator();
                if (it.MoveNext())
                    return it.Current.GetSchemaId() == LogonTemplateId;
            }
            return false;
        }

        private bool IsPacketWithLogout(MdpPacket mdpPacket)
        {
            var it = mdpPacket.GetEnumerator();
            return it.MoveNext() && it.Current.GetSchemaId() == LogoutTemplateId;
        }

        private static string PrepareLogonMessage(string username, string password)
        {
            string msg = $"35=A\u0001553={username}\u0001554={password}\u0001";
            msg = $"9={msg.Length}\u0001{msg}";
            return $"{msg}10={CalculateChecksum(msg)}\u0001";
        }

        private static string PrepareLogoutMessage()
        {
            string msg = "35=5\u0001";
            msg = $"9={msg.Length}\u0001{msg}";
            return $"{msg}10={CalculateChecksum(msg)}\u0001";
        }

        private static string PrepareRequestMessage(string channelId, long beginSeqNo, long endSeqNo, int mdReqId)
        {
            string msg = $"35=V\u00011180={channelId}\u0001262={channelId}-{mdReqId}\u00011182={beginSeqNo}\u00011183={endSeqNo}\u0001";
            msg = $"9={msg.Length}\u0001{msg}";
            return $"{msg}10={CalculateChecksum(msg)}\u0001";
        }

        private static string CalculateChecksum(string message)
        {
            int checksum = 0;
            foreach (char c in message) checksum += c;
            checksum &= 255;
            return checksum.ToString().PadLeft(3, '0');
        }
    }
}
