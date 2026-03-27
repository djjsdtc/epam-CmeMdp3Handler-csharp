using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Sbe.Message;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Core.Channel
{
    // Replaces Java NIO DatagramChannel+Selector multicast receive loop.
    // Uses a .NET Socket with UDP multicast and blocking ReceiveFrom in a Thread.
    public class MdpFeedWorker
    {
        private static readonly ILogger _logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MdpFeedWorker>();

        public const int RCV_BUFFER_SIZE = 4 * 1024 * 1024;

        private readonly ConnectionCfg _cfg;
        private readonly string? _networkInterface;
        private readonly int _rcvBufSize;
        private readonly List<IMdpFeedListener> _listeners = new();

        // AtomicReference<MdpFeedRtmState> replaced by volatile field + Interlocked
        private volatile int _feedState = (int)MdpFeedRtmState.STOPPED;

        private Socket? _socket;
        private MdpFeedContext? _feedContext;

        public MdpFeedWorker(ConnectionCfg cfg) : this(cfg, null, RCV_BUFFER_SIZE) { }

        public MdpFeedWorker(ConnectionCfg cfg, string? networkInterface, int rcvBufSize)
        {
            _cfg = cfg;
            _networkInterface = networkInterface;
            _rcvBufSize = rcvBufSize;
            _feedContext = new MdpFeedContext(cfg);
        }

        public void AddListener(IMdpFeedListener listener) => _listeners.Add(listener);

        public ConnectionCfg GetCfg() => _cfg;

        private bool TrySetState(MdpFeedRtmState expected, MdpFeedRtmState next) =>
            Interlocked.CompareExchange(ref _feedState, (int)next, (int)expected) == (int)expected;

        private MdpFeedRtmState GetState() => (MdpFeedRtmState)_feedState;

        public bool IsActive()      => GetState() == MdpFeedRtmState.ACTIVE;
        public bool IsShutdown()    => GetState() is MdpFeedRtmState.PENDING_SHUTDOWN or MdpFeedRtmState.SHUTDOWN;
        public bool IsActiveAndNotShutdown() => IsActive(); // review this later

        public bool CancelShutdownIfStarted() => TrySetState(MdpFeedRtmState.PENDING_SHUTDOWN, MdpFeedRtmState.ACTIVE);

        public bool Shutdown()
        {
            bool res = TrySetState(MdpFeedRtmState.ACTIVE, MdpFeedRtmState.PENDING_SHUTDOWN);
            if (res) _socket?.Close(); // wakes up blocking receive
            return res;
        }

        public void Run()
        {
            // Wait while previous instance is still shutting down
            while (IsShutdown()) Thread.Sleep(1);

            if (!TrySetState(MdpFeedRtmState.STOPPED, MdpFeedRtmState.ACTIVE)) return;

            // if any thread in result of concurrency already opened the feed, skip
            try { Open(); }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to open Feed");
                // if impossible to switch from stopped state to active, another thread is handling it
                TrySetState(MdpFeedRtmState.ACTIVE, MdpFeedRtmState.STOPPED);
                return;
            }

            NotifyStarted();

            var buf    = new byte[SbeConstants.MDP_PACKET_MAX_SIZE];
            var packet = MdpPacket.Instance();

            // work while any thread really started shutdown
            while (!TrySetState(MdpFeedRtmState.PENDING_SHUTDOWN, MdpFeedRtmState.SHUTDOWN))
            {
                try { ReceiveAndNotify(buf, packet); }
                catch (SocketException) { /* socket closed on shutdown */ break; }
                catch (Exception e) { _logger.LogError(e, "Exception in message loop"); }
            }

            // finally stop the feed thread
            try
            {
                Close();
                packet.Release();
                NotifyStopped();
                TrySetState(MdpFeedRtmState.SHUTDOWN, MdpFeedRtmState.STOPPED);
            }
            catch (Exception e) { _logger.LogError(e, "Failed to stop Feed"); }
        }

        private void Open()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, _rcvBufSize);
            _socket.Bind(new IPEndPoint(IPAddress.Any, _cfg.Port));

            var groupAddress = IPAddress.Parse(_cfg.Ip);
            IPAddress localAddr = IPAddress.Any;
            if (_networkInterface != null)
            {
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.Name == _networkInterface)
                    {
                        foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            { localAddr = addr.Address; break; }
                        break;
                    }
                }
            }
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership,
                new MulticastOption(groupAddress, localAddr));
        }

        private void Close()
        {
            _socket?.Close();
            _socket = null;
        }

        private void ReceiveAndNotify(byte[] buf, MdpPacket packet)
        {
            if (_socket == null) return;
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            int received = _socket.ReceiveFrom(buf, ref ep);
            if (received > 0)
            {
                packet.Buffer().WrapForParse(buf, received);
                packet.Length(received);
                NotifyListeners(packet);
            }
        }

        private void NotifyListeners(MdpPacket packet)
        {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].OnPacket(_feedContext!, packet);
        }

        private void NotifyStarted()
        {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].OnFeedStarted(_cfg.FeedType, _cfg.Feed);
        }

        private void NotifyStopped()
        {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i].OnFeedStopped(_cfg.FeedType, _cfg.Feed);
        }
    }
}
