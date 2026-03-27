using System;
using System.Collections.Generic;
using System.Threading;
using Epam.CmeMdp3Handler.Control;
using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Epam.CmeMdp3Handler.Service;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.Channel
{
    public class MdpChannelImpl : IMdpChannel
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<MdpChannelImpl>();

        private const int FeedIdleCheckDelayMs = 100_000; // 100 seconds in ms
        private const byte PlatformDefaultBookDepth = 10; // do we need an external option?

        internal readonly ChannelCfg _channelCfg;
        private readonly IDisposable? _schedulerHandle;
        private int _rcvBufSize = MdpFeedWorker.RCV_BUFFER_SIZE;

        private MdpFeedWorker? _incrementalFeedA;
        private MdpFeedWorker? _incrementalFeedB;
        private MdpFeedWorker? _snapshotFeedA;
        private MdpFeedWorker? _snapshotFeedB;
        private MdpFeedWorker? _instrumentFeedA;
        private MdpFeedWorker? _instrumentFeedB;

        private Thread? _incrementalFeedAThread;
        private Thread? _incrementalFeedBThread;
        private Thread? _snapshotFeedAThread;
        private Thread? _snapshotFeedBThread;
        private Thread? _instrumentFeedAThread;
        private Thread? _instrumentFeedBThread;

        private string? _incrementalFeedAni;
        private string? _incrementalFeedBni;
        private string? _snapshotFeedAni;
        private string? _snapshotFeedBni;
        private string? _instrumentFeedAni;
        private string? _instrumentFeedBni;

        private volatile Feed _snptFeedToUse = Feed.A;

        private readonly IMdpFeedListener _mdpFeedListener;
        internal readonly ChannelInstruments Instruments;

        private readonly List<IChannelListener> _listeners = new();
        private readonly List<IMarketDataListener> _mdListeners = new();
        private bool _hasMdListener = false;

        private readonly ChannelContext _channelContext;
        private long _idleWindowInMillis = FeedIdleCheckDelayMs;

        private bool _allSecuritiesMode = false;
        private byte _defMaxBookDepth = PlatformDefaultBookDepth;
        private int _defSubscriptionOptions = MdEventFlags.MESSAGE;

        private readonly ChannelController _channelController;
        private int _queueSlotInitBufferSize = InstrumentController.DefQueueSlotInitBufferSize;
        private int _incrQueueSize = InstrumentController.DefIncrQueueSize;
        private int _gapThreshold = InstrumentController.DefGapThreshold;

        internal MdpChannelImpl(DefaultScheduledServiceHolder.SchedulerHolder? scheduler,
            ChannelCfg channelCfg, MdpMessageTypes mdpMessageTypes,
            int queueSlotInitBufferSize, int incrQueueSize, int gapThreshold)
        {
            _channelCfg = channelCfg;
            _gapThreshold = gapThreshold;
            _channelContext = new ChannelContext(this, mdpMessageTypes, _gapThreshold);
            Instruments = new ChannelInstruments(_channelContext);
            _queueSlotInitBufferSize = queueSlotInitBufferSize;
            _incrQueueSize = incrQueueSize;
            _channelController = new ChannelController(_channelContext, _incrQueueSize, _queueSlotInitBufferSize);
            _mdpFeedListener = new MdpFeedListenerImpl(this);
            if (scheduler != null)
                _schedulerHandle = scheduler.ScheduleWithFixedDelay(CheckFeedIdleState, FeedIdleCheckDelayMs, FeedIdleCheckDelayMs);
        }

        public string GetId() => _channelCfg.Id;
        public int GetGapThreshold() => _gapThreshold;
        public void SetGapThreshold(int gapThreshold) => _gapThreshold = gapThreshold;
        public void SetIdleWindowInMillis(long ms) => _idleWindowInMillis = ms;

        public void SetSnapshotFeedAni(string? v) => _snapshotFeedAni = v;
        public void SetSnapshotFeedBni(string? v) => _snapshotFeedBni = v;
        public void SetIncrementalFeedAni(string? v) => _incrementalFeedAni = v;
        public void SetIncrementalFeedBni(string? v) => _incrementalFeedBni = v;
        public void SetInstrumentFeedAni(string? v) => _instrumentFeedAni = v;
        public void SetInstrumentFeedBni(string? v) => _instrumentFeedBni = v;
        public void SetRcvBufSize(int size) => _rcvBufSize = size;

        private static void WaitForThread(Thread? thread)
        {
            if (thread != null && thread.IsAlive)
                thread.Join();
        }

        public void Close()
        {
            _channelController.Lock();
            try { _channelController.SwitchState(ChannelState.CLOSING); }
            finally { _channelController.Unlock(); }

            StopAllFeeds();
            try
            {
                WaitForThread(_incrementalFeedAThread);
                WaitForThread(_incrementalFeedBThread);
                WaitForThread(_snapshotFeedAThread);
                WaitForThread(_snapshotFeedBThread);
                WaitForThread(_instrumentFeedAThread);
                WaitForThread(_instrumentFeedBThread);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to stop Feed Worker: {Msg}", e.Message);
            }

            _schedulerHandle?.Dispose();

            _channelController.Lock();
            try
            {
                _channelController.Close();
                _channelController.SwitchState(ChannelState.CLOSED);
            }
            finally { _channelController.Unlock(); }
        }

        public void EnableAllSecuritiesMode() => _allSecuritiesMode = true;
        public void DisableAllSecuritiesMode() => _allSecuritiesMode = false;
        public byte GetDefMaxBookDepth() => _defMaxBookDepth;
        public void SetDefMaxBookDepth(byte v) => _defMaxBookDepth = v;
        public int GetDefSubscriptionOptions() => _defSubscriptionOptions;
        public void SetDefSubscriptionOptions(int v) => _defSubscriptionOptions = v;
        public ChannelState GetState() => _channelController.GetState();
        public void SetStateForcibly(ChannelState state) => _channelController.SwitchState(state);
        public ChannelController GetController() => _channelController;

        private void CheckFeedIdleState()
        {
            lock (this)
            {
                long allowedInactiveEndTime = _channelController.GetLastIncrPcktReceived() + _idleWindowInMillis;
                if (allowedInactiveEndTime < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() &&
                    (_incrementalFeedA?.IsActiveAndNotShutdown() == true || _incrementalFeedB?.IsActiveAndNotShutdown() == true))
                {
                    _channelController.Lock();
                    try
                    {
                        if (_channelController.GetState() != ChannelState.CLOSING &&
                            _channelController.GetState() != ChannelState.CLOSED)
                            StartSnapshotFeeds();
                    }
                    finally { _channelController.Unlock(); }
                }
            }
        }

        public void RegisterListener(IChannelListener channelListener)
        {
            if (channelListener != null)
                lock (_listeners) { _listeners.Add(channelListener); }
        }

        public void RemoveListener(IChannelListener channelListener)
        {
            if (channelListener != null)
                lock (_listeners) { _listeners.Remove(channelListener); }
        }

        public void RegisterMarketDataListener(IMarketDataListener mdListener)
        {
            lock (_mdListeners) { _mdListeners.Add(mdListener); SetMdEnabledFlag(); }
        }

        public void RemoveMarketDataListener(IMarketDataListener mdListener)
        {
            lock (_mdListeners) { _mdListeners.Remove(mdListener); SetMdEnabledFlag(); }
        }

        private void SetMdEnabledFlag() => _hasMdListener = _mdListeners.Count > 0;
        public bool HasMdListener() => _hasMdListener;

        public List<IChannelListener> GetListeners() => _listeners;
        public List<IMarketDataListener> GetMdListeners() => _mdListeners;

        public void StartIncrementalFeedA()
        {
            if (_incrementalFeedA == null)
                lock (this)
                    if (_incrementalFeedA == null)
                    {
                        _incrementalFeedA = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.I, Feed.A)!, _incrementalFeedAni, _rcvBufSize);
                        _incrementalFeedA.AddListener(_mdpFeedListener);
                    }
            if (!_incrementalFeedA.CancelShutdownIfStarted() && !_incrementalFeedA.IsActive())
            {
                _incrementalFeedAThread = new Thread(_incrementalFeedA.Run) { IsBackground = true };
                _incrementalFeedAThread.Start();
            }
        }

        public void StartIncrementalFeedB()
        {
            if (_incrementalFeedB == null)
                lock (this)
                    if (_incrementalFeedB == null)
                    {
                        _incrementalFeedB = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.I, Feed.B)!, _incrementalFeedBni, _rcvBufSize);
                        _incrementalFeedB.AddListener(_mdpFeedListener);
                    }
            if (!_incrementalFeedB.CancelShutdownIfStarted() && !_incrementalFeedB.IsActive())
            {
                _incrementalFeedBThread = new Thread(_incrementalFeedB.Run) { IsBackground = true };
                _incrementalFeedBThread.Start();
            }
        }

        public void StartSnapshotFeedA()
        {
            if (_snapshotFeedA == null)
                lock (this)
                    if (_snapshotFeedA == null)
                    {
                        _snapshotFeedA = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.S, Feed.A)!, _snapshotFeedAni, _rcvBufSize);
                        _snapshotFeedA.AddListener(_mdpFeedListener);
                    }
            if (!_snapshotFeedA.CancelShutdownIfStarted() && !_snapshotFeedA.IsActive())
            {
                _snapshotFeedAThread = new Thread(_snapshotFeedA.Run) { IsBackground = true };
                _snapshotFeedAThread.Start();
            }
        }

        public void StartSnapshotFeedB()
        {
            if (_snapshotFeedB == null)
                lock (this)
                    if (_snapshotFeedB == null)
                    {
                        _snapshotFeedB = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.S, Feed.B)!, _snapshotFeedBni, _rcvBufSize);
                        _snapshotFeedB.AddListener(_mdpFeedListener);
                    }
            if (!_snapshotFeedB.CancelShutdownIfStarted() && !_snapshotFeedB.IsActive())
            {
                _snapshotFeedBThread = new Thread(_snapshotFeedB.Run) { IsBackground = true };
                _snapshotFeedBThread.Start();
            }
        }

        public void StartInstrumentFeedA()
        {
            if (_instrumentFeedA == null)
                lock (this)
                    if (_instrumentFeedA == null)
                    {
                        _instrumentFeedA = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.N, Feed.A)!, _instrumentFeedAni, _rcvBufSize);
                        _instrumentFeedA.AddListener(_mdpFeedListener);
                    }
            if (!_instrumentFeedA.CancelShutdownIfStarted() && !_instrumentFeedA.IsActive())
            {
                _instrumentFeedAThread = new Thread(_instrumentFeedA.Run) { IsBackground = true };
                _instrumentFeedAThread.Start();
            }
        }

        public void StartInstrumentFeedB()
        {
            if (_instrumentFeedB == null)
                lock (this)
                    if (_instrumentFeedB == null)
                    {
                        _instrumentFeedB = new MdpFeedWorker(_channelCfg.GetConnectionCfg(FeedType.N, Feed.B)!, _instrumentFeedBni, _rcvBufSize);
                        _instrumentFeedB.AddListener(_mdpFeedListener);
                    }
            if (!_instrumentFeedB.CancelShutdownIfStarted() && !_instrumentFeedB.IsActive())
            {
                _instrumentFeedBThread = new Thread(_instrumentFeedB.Run) { IsBackground = true };
                _instrumentFeedBThread.Start();
            }
        }

        public void StopIncrementalFeedA() { if (_incrementalFeedA?.IsActive() == true) _incrementalFeedA.Shutdown(); }
        public void StopIncrementalFeedB() { if (_incrementalFeedB?.IsActive() == true) _incrementalFeedB.Shutdown(); }
        public void StopSnapshotFeedA() { if (_snapshotFeedA?.IsActive() == true) _snapshotFeedA.Shutdown(); }
        public void StopSnapshotFeedB() { if (_snapshotFeedB?.IsActive() == true) _snapshotFeedB.Shutdown(); }
        public void StopInstrumentFeedA() { if (_instrumentFeedA?.IsActive() == true) _instrumentFeedA.Shutdown(); }
        public void StopInstrumentFeedB() { if (_instrumentFeedB?.IsActive() == true) _instrumentFeedB.Shutdown(); }

        public void StopAllFeeds()
        {
            StopIncrementalFeedA();
            StopIncrementalFeedB();
            StopSnapshotFeedA();
            StopSnapshotFeedB();
            StopInstrumentFeedA();
            StopInstrumentFeedB();
        }

        public void StartInstrumentFeeds()
        {
            try
            {
                Instruments.ResetCycleCounter();
                StartInstrumentFeedA();
                StartIncrementalFeedB();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to start Instrument Feeds: {Msg}", e.Message);
            }
        }

        internal bool IsSnapshotFeedsActive()
            => (_snapshotFeedA?.IsActive() == true) || (_snapshotFeedB?.IsActive() == true);

        public void StartSnapshotFeeds()
        {
            try
            {
                _channelController.ResetSnapshotCycleCount();
                if (_snptFeedToUse == Feed.A) StartSnapshotFeedA();
                else StartSnapshotFeedB();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to start Snapshot Feeds: {Msg}", e.Message);
            }
        }

        public void StopSnapshotFeeds()
        {
            StopSnapshotFeedA();
            StopSnapshotFeedB();
        }

        internal void SubscribeToSnapshotsForInstrument(int securityId)
        {
            _channelController.AddOutOfSyncInstrument(securityId);
            StartSnapshotFeeds();
        }

        internal void UnsubscribeFromSnapshotsForInstrument(int securityId)
        {
            if (_channelController.RemoveOutOfSyncInstrument(securityId))
            {
                if (IsSnapshotFeedsActive() && !_channelController.HasOutOfSyncInstruments())
                    StopSnapshotFeeds();
            }
        }

        internal InstrumentController? FindController(int securityId, string? secDesc)
            => Instruments.Find(securityId, secDesc, _allSecuritiesMode, _defSubscriptionOptions, _defMaxBookDepth);

        public bool Subscribe(int securityId, string? secDesc, int subscrFlags, byte depth)
            => Instruments.RegisterSecurity(securityId, secDesc, subscrFlags, depth);

        public bool SubscribeWithDefDepth(int securityId, string? secDesc, int subscrFlags)
            => Subscribe(securityId, secDesc, subscrFlags, _defMaxBookDepth);

        public bool Subscribe(int securityId, string? secDesc, byte depth)
            => Subscribe(securityId, secDesc, _defSubscriptionOptions, depth);

        public bool Subscribe(int securityId, string? secDesc)
            => SubscribeWithDefDepth(securityId, secDesc, _defSubscriptionOptions);

        public void DiscontinueSecurity(int securityId) => Instruments.DiscontinueSecurity(securityId);
        public int GetSubscriptionFlags(int securityId) => Instruments.GetSubscriptionFlags(securityId);
        public void SetSubscriptionFlags(int securityId, int flags) => Instruments.SetSubscriptionFlags(securityId, flags, _defMaxBookDepth);
        public void AddSubscriptionFlags(int securityId, int flags) => Instruments.AddSubscriptionFlags(securityId, flags, _defMaxBookDepth);
        public void RemoveSubscriptionFlags(int securityId, int flags) => Instruments.RemoveSubscriptionFlags(securityId, flags);

        public void HandlePacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            var feedType = feedContext.FeedType;
            var feed = feedContext.Feed;
            _channelContext.NotifyPacketReceived(feedType, feed, mdpPacket);
            if (feedType == FeedType.N)
                Instruments.OnPacket(feedContext, mdpPacket);
            else if (feedType == FeedType.I)
                _channelController.HandleIncrementalPacket(feedContext, mdpPacket);
            else if (feedType == FeedType.S)
                _channelController.HandleSnapshotPacket(feedContext, mdpPacket);
        }

        public int GetQueueSlotInitBufferSize() => _queueSlotInitBufferSize;
        public int GetIncrQueueSize() => _incrQueueSize;

        private sealed class MdpFeedListenerImpl : IMdpFeedListener
        {
            private readonly MdpChannelImpl _outer;
            public MdpFeedListenerImpl(MdpChannelImpl outer) => _outer = outer;

            public void OnFeedStarted(FeedType feedType, Feed feed)
                => _outer._channelContext.NotifyFeedStartedListeners(feedType, feed);

            public void OnFeedStopped(FeedType feedType, Feed feed)
                => _outer._channelContext.NotifyFeedStoppedListeners(feedType, feed);

            public void OnPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
                => _outer.HandlePacket(feedContext, mdpPacket);
        }
    }
}
