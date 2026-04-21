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
using System.Globalization;
using System.Threading;
using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Core.Channel.Tcp;
using Epam.CmeMdp3Handler.MbpWithMbo.Control;
using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Epam.CmeMdp3Handler.Service;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Channel
{
    /// <summary>
    /// Main channel implementation for MBP-with-MBO mode.
    /// Manages feed workers, routes packets, and coordinates gap recovery.
    ///
    /// Java: com.epam.cme.mdp3.channel.LowLevelMdpChannel
    /// C# note: Java ScheduledExecutorService.scheduleWithFixedDelay -> System.Threading.Timer.
    ///          Java MutablePair (Apache Commons) -> ValueTuple (MdpFeedWorker, Thread?).
    ///          Java Consumer&lt;MdpMessage&gt; emptyBookConsumers -> List&lt;Action&lt;IMdpMessage&gt;&gt;.
    ///          Java DecimalFormat -> string.Format with custom precision.
    ///          Java AtomicInteger msgCountDown -> volatile int + Interlocked operations.
    /// </summary>
    internal class LowLevelMdpChannel : IMdpChannel
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<LowLevelMdpChannel>();

        private const int FeedIdleCheckDelay = 100;          // seconds
        private const int FeedIdleCheckDelayMs = FeedIdleCheckDelay * 1000;

        private readonly ChannelCfg _channelCfg;
        private readonly DefaultScheduledServiceHolder.SchedulerHolder? _scheduler;
        private int _rcvBufSize;

        // Feed workers: FeedType -> (worker, thread)
        private readonly Dictionary<FeedType, (MdpFeedWorker worker, Thread? thread)> _feedsA = new();
        private readonly Dictionary<FeedType, (MdpFeedWorker worker, Thread? thread)> _feedsB = new();
        private readonly Dictionary<FeedType, string?> _feedANetworkInterfaces;
        private readonly Dictionary<FeedType, string?> _feedBNetworkInterfaces;

        private volatile Feed _snptFeedToUse = Feed.A;
        private readonly FeedListenerImpl _feedListener;
        private readonly List<IChannelListener> _listeners = new();
        private readonly long _idleWindowInMillis = FeedIdleCheckDelayMs;
        private readonly GapChannelController _channelController;
        private IDisposable? _checkFeedIdleStateFuture;
        private long _lastIncrPcktReceived = 0;
        private readonly IInstrumentManager _instrumentManager;
        private readonly MdpMessageTypes _mdpMessageTypes;
        private readonly bool _mboEnabled;
        private readonly InstrumentObserverImpl _instrumentObserver;
        private readonly GapChannelController.ISnapshotRecoveryManager _recoveryManager;
        private readonly IncrementalStatistics? _incrementalStatistics;

        private readonly object _feedLock = new object();

        internal LowLevelMdpChannel(
            DefaultScheduledServiceHolder.SchedulerHolder? scheduler,
            ChannelCfg channelCfg,
            MdpMessageTypes mdpMessageTypes,
            int incrQueueSize,
            int rcvBufSize,
            int gapThreshold,
            int maxNumberOfTcpAttempts,
            string tcpUsername,
            string tcpPassword,
            Dictionary<FeedType, string?> feedANetworkInterfaces,
            Dictionary<FeedType, string?> feedBNetworkInterfaces,
            bool mboEnabled,
            IList<int>? mboIncrementMessageTemplateIds,
            IList<int>? mboSnapshotMessageTemplateIds,
            Feed snptFeedToUse,
            int outputStatisticsEveryXseconds)
        {
            _scheduler = scheduler;
            _channelCfg = channelCfg;
            _rcvBufSize = rcvBufSize;
            _feedANetworkInterfaces = feedANetworkInterfaces;
            _feedBNetworkInterfaces = feedBNetworkInterfaces;
            _mdpMessageTypes = mdpMessageTypes;
            _mboEnabled = mboEnabled;
            _snptFeedToUse = snptFeedToUse;

            _incrementalStatistics = outputStatisticsEveryXseconds > 0
                ? new IncrementalStatistics(outputStatisticsEveryXseconds)
                : null;

            string channelId = channelCfg.Id;
            _instrumentManager = new MdpInstrumentManager(channelId, _listeners);

            IMdpOffHeapBuffer buffer = new MdpOffHeapBuffer(incrQueueSize);

            List<Action<IMdpMessage>> emptyBookConsumers = new();

            _instrumentObserver = new InstrumentObserverImpl(this);

            IChannelController target = new ChannelControllerRouter(channelId, _instrumentManager, mdpMessageTypes,
                _listeners, _instrumentObserver, emptyBookConsumers, mboIncrementMessageTemplateIds, mboSnapshotMessageTemplateIds);

            ISnapshotCycleHandler mbpCycleHandler = new OffHeapSnapshotCycleHandler();
            ISnapshotCycleHandler mboCycleHandler;
            FeedType recoveryFeedType;

            if (mboEnabled)
            {
                recoveryFeedType = FeedType.SMBO;
                mboCycleHandler = new OffHeapSnapshotCycleHandler();
            }
            else
            {
                recoveryFeedType = FeedType.S;
                mboCycleHandler = mbpCycleHandler;
            }

            _recoveryManager = GetRecoveryManager(recoveryFeedType);

            ITcpMessageRequester? tcpMessageRequester = null;
            ConnectionCfg? connectionCfg = channelCfg.GetConnectionCfg(FeedType.H, Feed.A);
            if (connectionCfg != null)
            {
                ITcpChannel tcpChannel = new MdpTcpChannel(connectionCfg);
                tcpMessageRequester = new MdpTcpMessageRequester(channelId, _listeners, mdpMessageTypes, tcpChannel, tcpUsername, tcpPassword);
            }

            _channelController = new GapChannelController(_listeners, target, _recoveryManager, buffer, gapThreshold,
                maxNumberOfTcpAttempts, channelId, mdpMessageTypes, mboCycleHandler, mbpCycleHandler,
                tcpMessageRequester, mboIncrementMessageTemplateIds, mboSnapshotMessageTemplateIds);

            emptyBookConsumers.Add(_channelController.Accept);

            _feedListener = new FeedListenerImpl(this);

            if (scheduler != null)
                InitChannelStateThread();
        }

        public string GetId() => _channelCfg.Id;

        public void Close()
        {
            _checkFeedIdleStateFuture?.Dispose();
            _channelController.PreClose();
            StopAllFeeds();
            foreach (var entry in _feedsA.Values) CloseFeed(entry);
            foreach (var entry in _feedsB.Values) CloseFeed(entry);
            _channelController.Close();
        }

        public ChannelState GetState() => _channelController.GetState();

        private void InitChannelStateThread()
        {
            _checkFeedIdleStateFuture = _scheduler!.ScheduleWithFixedDelay(
                CheckFeedIdleState,
                FeedIdleCheckDelayMs,
                FeedIdleCheckDelayMs);
        }

        public void RegisterListener(IChannelListener channelListener)
        {
            if (channelListener != null)
            {
                lock (_listeners) { _listeners.Add(channelListener); }
            }
        }

        public void RemoveListener(IChannelListener channelListener)
        {
            if (channelListener != null)
            {
                lock (_listeners) { _listeners.Remove(channelListener); }
            }
        }

        public IList<IChannelListener> GetListeners() => _listeners;

        public void StopAllFeeds()
        {
            StopFeed(FeedType.I, Feed.A);
            StopFeed(FeedType.I, Feed.B);
            StopFeed(_mboEnabled ? FeedType.SMBO : FeedType.S, Feed.A);
            StopFeed(_mboEnabled ? FeedType.SMBO : FeedType.S, Feed.B);
            StopFeed(FeedType.H, Feed.A);
            StopFeed(FeedType.H, Feed.B);
        }

        public bool Subscribe(int securityId, string? secDesc)
        {
            _instrumentManager.RegisterSecurity(securityId, secDesc);
            return true;
        }

        public void DiscontinueSecurity(int securityId)
        {
            _instrumentManager.DiscontinueSecurity(securityId);
        }

        public void StartFeed(FeedType feedType, Feed feed)
        {
            Dictionary<FeedType, (MdpFeedWorker, Thread?)> currentFeed;
            Dictionary<FeedType, string?> networkInterfaces;

            if (_mboEnabled && feedType == FeedType.S)
                throw new Core.Channel.MdpFeedException("It is not allowed to use MBP snapshot feed when MBO is enabled");

            if (feed == Feed.A)
            {
                currentFeed = _feedsA;
                networkInterfaces = _feedANetworkInterfaces;
            }
            else if (feed == Feed.B)
            {
                currentFeed = _feedsB;
                networkInterfaces = _feedBNetworkInterfaces;
            }
            else
            {
                throw new ArgumentException($"{feed} feed is not supported");
            }

            if (!currentFeed.ContainsKey(feedType))
            {
                lock (_feedLock)
                {
                    if (!currentFeed.ContainsKey(feedType))
                    {
                        ConnectionCfg? cfg = _channelCfg.GetConnectionCfg(feedType, feed);
                        if (cfg == null) return;
                        networkInterfaces.TryGetValue(feedType, out string? ni);
                        var worker = new MdpFeedWorker(cfg, ni, _rcvBufSize);
                        worker.AddListener(_feedListener);
                        currentFeed[feedType] = (worker, null);
                    }
                }
            }

            var (mdpFeedWorker, thread) = currentFeed[feedType];
            if (!mdpFeedWorker.CancelShutdownIfStarted())
            {
                if (!mdpFeedWorker.IsActive())
                {
                    var newThread = new Thread(mdpFeedWorker.Run) { IsBackground = true };
                    currentFeed[feedType] = (mdpFeedWorker, newThread);
                    newThread.Start();
                }
            }
        }

        private void CheckFeedIdleState()
        {
            try
            {
                lock (_feedLock)
                {
                    if (!_feedsA.TryGetValue(FeedType.I, out var fA) || !_feedsB.TryGetValue(FeedType.I, out var fB))
                        return;
                    long allowedInactiveEndTime = Interlocked.Read(ref _lastIncrPcktReceived) + _idleWindowInMillis;
                    if (allowedInactiveEndTime < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() &&
                        (fA.worker.IsActiveAndNotShutdown() || fB.worker.IsActiveAndNotShutdown()))
                    {
                        _recoveryManager.StartRecovery();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "{Message}", e.Message);
            }
        }

        public void StopFeed(FeedType feedType, Feed feed)
        {
            Dictionary<FeedType, (MdpFeedWorker, Thread?)> currentFeed;
            if (feed == Feed.A) currentFeed = _feedsA;
            else if (feed == Feed.B) currentFeed = _feedsB;
            else throw new ArgumentException($"{feed} feed is not supported");

            if (currentFeed.TryGetValue(feedType, out var pair))
            {
                if (pair.Item1.IsActive())
                    pair.Item1.Shutdown();
            }
        }

        private void CloseFeed((MdpFeedWorker worker, Thread? thread) feedEntry)
        {
            try
            {
                Thread? thread = feedEntry.thread;
                if (thread != null && thread.IsAlive)
                {
                    try { thread.Join(1000); }
                    catch (ThreadInterruptedException e)
                    {
                        Logger.LogError(e, "Timed out waiting to stop Feed Worker: {Message}", e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to stop Feed Worker: {Message}", e.Message);
            }
        }

        private GapChannelController.ISnapshotRecoveryManager GetRecoveryManager(FeedType feedType)
        {
            return new RecoveryManagerImpl(this, feedType);
        }

        private sealed class RecoveryManagerImpl : GapChannelController.ISnapshotRecoveryManager
        {
            private readonly LowLevelMdpChannel _channel;
            private readonly FeedType _feedType;

            public RecoveryManagerImpl(LowLevelMdpChannel channel, FeedType feedType)
            {
                _channel = channel;
                _feedType = feedType;
            }

            public void StartRecovery()
            {
                try { _channel.StartFeed(_feedType, _channel._snptFeedToUse); }
                catch (Exception e) { Logger.LogError(e, "{Message}", e.Message); }
            }

            public void StopRecovery() => _channel.StopFeed(_feedType, _channel._snptFeedToUse);
        }

        private sealed class FeedListenerImpl : IMdpFeedListener
        {
            private readonly LowLevelMdpChannel _channel;

            public FeedListenerImpl(LowLevelMdpChannel channel) { _channel = channel; }

            public void OnFeedStarted(FeedType feedType, Feed feed)
            {
                foreach (var l in _channel._listeners) l.OnFeedStarted(_channel.GetId(), feedType, feed);
            }

            public void OnFeedStopped(FeedType feedType, Feed feed)
            {
                foreach (var l in _channel._listeners) l.OnFeedStopped(_channel.GetId(), feedType, feed);
            }

            public void OnPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
            {
                FeedType feedType = feedContext.FeedType;
                Feed feed = feedContext.Feed;

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("New MDP Packet: #{SeqNum} from Feed {FeedType}:{Feed}", mdpPacket.GetMsgSeqNum(), feedType, feed);

                foreach (var l in _channel._listeners)
                    l.OnPacket(_channel.GetId(), feedType, feed, mdpPacket);

                if (feedType == FeedType.N)
                {
                    _channel._instrumentObserver.OnPacket(feedContext, mdpPacket);
                }
                else if (feedType == FeedType.I)
                {
                    if (_channel._incrementalStatistics != null && Logger.IsEnabled(LogLevel.Information))
                        _channel._incrementalStatistics.Update(feed, mdpPacket.Buffer().Length());

                    Interlocked.Exchange(ref _channel._lastIncrPcktReceived, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    _channel._channelController.HandleIncrementalPacket(feedContext, mdpPacket);
                }
                else if (feedType == FeedType.SMBO || feedType == FeedType.S)
                {
                    _channel._channelController.HandleSnapshotPacket(feedContext, mdpPacket);
                }
            }
        }

        private sealed class InstrumentObserverImpl : IInstrumentObserver
        {
            private const int PrcdMsgCountNull = int.MaxValue; // max value used as undefined (null)
            private const int InstrumentCyclesMax = 2; // do we need an option in configuration for this?

            private int _msgCountDown = PrcdMsgCountNull; // volatile int + Interlocked replaces AtomicInteger
            private readonly SbeString _strValObj = SbeString.Allocate(256);
            private readonly LowLevelMdpChannel _channel;

            public InstrumentObserverImpl(LowLevelMdpChannel channel) { _channel = channel; }

            public void OnPacket(MdpFeedContext feedContext, MdpPacket instrumentPacket)
            {
                foreach (IMdpMessage mdpMessage in instrumentPacket)
                {
                    MdpMessageType messageType = _channel._mdpMessageTypes.GetMessageType(mdpMessage.GetSchemaId());
                    SemanticMsgType? semanticMsgType = messageType.GetSemanticMsgType();
                    if (semanticMsgType == SemanticMsgType.SecurityDefinition)
                        mdpMessage.SetMessageType(messageType);
                    OnMessage(feedContext, mdpMessage);
                }
            }

            public void OnMessage(MdpFeedContext feedContext, IMdpMessage secDefMsg)
            {
                int subscriptionFlags = NotifySecurityDefinitionListeners(secDefMsg);
                int securityId = secDefMsg.GetInt32(MdConstants.SECURITY_ID);

                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug("Subscription flags for channel '{ChannelId}' and instrument '{SecurityId}' are '{Flags}'",
                        _channel.GetId(), securityId, subscriptionFlags);

                string? secDesc = null;
                if (secDefMsg.GetString(MdConstants.SEC_DESC_TAG, _strValObj))
                    secDesc = _strValObj.GetString();

                if (MdEventFlags.HasMessage(subscriptionFlags))
                    _channel._instrumentManager.RegisterSecurity(securityId, secDesc);
                else
                    _channel._instrumentManager.UpdateSecDesc(securityId, secDesc);

                if (Volatile.Read(ref _msgCountDown) == PrcdMsgCountNull)
                {
                    int totalNumReports = GetTotalReportNum(secDefMsg) * InstrumentCyclesMax;
                    Interlocked.CompareExchange(ref _msgCountDown, totalNumReports, PrcdMsgCountNull);
                }

                int msgLeft = Interlocked.Decrement(ref _msgCountDown);
                if (CanStopInstrumentListening(msgLeft))
                {
                    _channel.StopFeed(FeedType.N, Feed.A);
                    _channel.StopFeed(FeedType.N, Feed.B);
                }
            }

            private int NotifySecurityDefinitionListeners(IMdpMessage mdpMessage)
            {
                int flags = MdEventFlags.NOTHING;
                foreach (IChannelListener listener in _channel._listeners)
                    flags |= listener.OnSecurityDefinition(_channel.GetId(), mdpMessage);
                return flags;
            }

            private static int GetTotalReportNum(IMdpMessage mdpMessage) =>
                (int)mdpMessage.GetUInt32(MdConstants.TOT_NUM_REPORTS);

            private static bool CanStopInstrumentListening(int cyclesLeft) => cyclesLeft <= 0;
        }

        /// <summary>
        /// Tracks and logs per-feed incremental packet statistics.
        ///
        /// Java: LowLevelMdpChannel.IncrementalStatistics (private inner class)
        /// C# note: Java DecimalFormat("#.000") -> string.Format with "F3" format specifier.
        /// </summary>
        private sealed class IncrementalStatistics
        {
            private readonly int[] _readCount = { 0, 0 };
            private readonly long[] _dataCount = { 0, 0 };
            private readonly long[] _time = { DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            private readonly int _outputEveryXSeconds;
            private readonly int _outputEveryXMilliseconds;

            public IncrementalStatistics(int outputEveryXSeconds)
            {
                _outputEveryXSeconds = outputEveryXSeconds;
                _outputEveryXMilliseconds = outputEveryXSeconds * 1000;
            }

            /// <summary>
            /// Update the current read and data counts and check if the duration is past the marker
            /// and then output the statistics to the log and reset.
            /// </summary>
            /// <param name="feed">The current feed to update statistics for</param>
            /// <param name="dataLength">The current data length to add to the internal data length count</param>
            public void Update(Feed feed, long dataLength)
            {
                int index = feed == Feed.A ? 0 : 1;
                _readCount[index]++;
                _dataCount[index] += dataLength;
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (now - _time[index] > _outputEveryXMilliseconds)
                {
                    double dataMbps = (((_dataCount[index] / 1000.0 / 1000.0) * 8) / _outputEveryXSeconds);
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInformation("Read {Packets} packets and {Mbps} Mbps in {Ms} ms on Feed {Feed}",
                            _readCount[index], dataMbps.ToString("F3", CultureInfo.InvariantCulture),
                            now - _time[index], feed);
                    }
                    _time[index] = now;
                    _readCount[index] = 0;
                    _dataCount[index] = 0;
                }
            }
        }
    }
}
