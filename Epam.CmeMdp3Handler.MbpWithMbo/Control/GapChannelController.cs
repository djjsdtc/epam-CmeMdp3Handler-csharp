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
using System.Threading;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Core.Channel.Tcp;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Detects and recovers from sequence-number gaps using either snapshot recovery or
    /// TCP replay.
    ///
    /// Java: com.epam.cme.mdp3.control.GapChannelController
    /// C# note: Java ReentrantLock -> C# lock statement.
    ///          Java ScheduledExecutorService.execute() -> ThreadPool.QueueUserWorkItem().
    ///          Java Consumer&lt;MdpMessage&gt; (implements) -> Action&lt;IMdpMessage&gt; via Accept() method.
    /// </summary>
    public class GapChannelController : IMdpChannelController
    {
        private static readonly ILogger Log =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GapChannelController>();

        public const int MaxNumberOfTcpAttempts = 3;

        private readonly object _lock = new object();
        private readonly int _gapThreshold;
        private readonly int _maxNumberOfTcpAttempts;
        private readonly IMdpOffHeapBuffer _buffer;
        private readonly ISnapshotRecoveryManager _snapshotRecoveryManager;
        private readonly IChannelController _target;
        private readonly string _channelId;
        private readonly ISnapshotCycleHandler _mboCycleHandler;
        private readonly ISnapshotCycleHandler _mbpCycleHandler;
        private long _lastProcessedSeqNum;
        private long _smallestSnapshotSequence;
        private long _highestSnapshotSequence;
        private bool _wasChannelResetInPrcdPacket;
        private ChannelState _currentState = ChannelState.INITIAL;
        private readonly MdpMessageTypes _mdpMessageTypes;
        private bool _receivingCycle = false;
        private readonly IList<IChannelListener> _channelListeners;
        private readonly TcpRecoveryProcessor? _tcpRecoveryProcessor;
        private int _numberOfTcpAttempts;
        private long _packetsInBufferDuringInitialOrOutOfSync = 0;
        private readonly IList<int>? _mboIncrementMessageTemplateIds;
        private readonly IList<int>? _mboSnapshotMessageTemplateIds;

        public GapChannelController(IList<IChannelListener> channelListeners, IChannelController target,
            ISnapshotRecoveryManager snapshotRecoveryManager, IMdpOffHeapBuffer buffer, int gapThreshold,
            int maxNumberOfTcpAttempts, string channelId, MdpMessageTypes mdpMessageTypes,
            ISnapshotCycleHandler mboCycleHandler, ISnapshotCycleHandler mbpCycleHandler,
            ITcpMessageRequester? tcpMessageRequester,
            IList<int>? mboIncrementMessageTemplateIds, IList<int>? mboSnapshotMessageTemplateIds)
        {
            _channelListeners = channelListeners;
            _buffer = buffer;
            _snapshotRecoveryManager = snapshotRecoveryManager;
            _target = target;
            _gapThreshold = gapThreshold;
            _maxNumberOfTcpAttempts = maxNumberOfTcpAttempts;
            _channelId = channelId;
            _mdpMessageTypes = mdpMessageTypes;
            _mboCycleHandler = mboCycleHandler;
            _mbpCycleHandler = mbpCycleHandler;
            if (tcpMessageRequester != null)
            {
                ITcpPacketListener tcpPacketListener = new TcpPacketListenerImpl(this);
                _tcpRecoveryProcessor = new TcpRecoveryProcessor(tcpMessageRequester, tcpPacketListener, this);
            }
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
            long pkgSequence = mdpPacket.GetMsgSeqNum();
            if (Log.IsEnabled(LogLevel.Trace))
            {
                Log.LogTrace("Feed {FeedType}:{Feed} | HandleSnapshotPacket: previous processed sequence '{Prev}', current packet's sequence '{Curr}'",
                    feedContext.FeedType, feedContext.Feed, _lastProcessedSeqNum, pkgSequence);
            }
            lock (_lock)
            {
                if (mdpPacket.GetMsgSeqNum() == 1)
                {
                    if (_receivingCycle)
                    {
                        _smallestSnapshotSequence = _mboCycleHandler.GetSmallestSnapshotSequence();
                        _highestSnapshotSequence = _mboCycleHandler.GetHighestSnapshotSequence();
                        if (_smallestSnapshotSequence != ISnapshotCycleHandler.SnapshotSequenceUndefined
                            && _highestSnapshotSequence != ISnapshotCycleHandler.SnapshotSequenceUndefined
                            && _mbpCycleHandler.GetSmallestSnapshotSequence() != ISnapshotCycleHandler.SnapshotSequenceUndefined
                            && _mbpCycleHandler.GetHighestSnapshotSequence() != ISnapshotCycleHandler.SnapshotSequenceUndefined)
                        {
                            if (_mbpCycleHandler.GetSmallestSnapshotSequence() != _smallestSnapshotSequence
                                || _mbpCycleHandler.GetHighestSnapshotSequence() != _highestSnapshotSequence)
                            {
                                Log.LogError("MBP(Highest '{MbpHigh}', Smallest '{MbpSmall}') and MBO(Highest '{MboHigh}', Smallest '{MboSmall}') snapshots are not synchronized",
                                    _mbpCycleHandler.GetHighestSnapshotSequence(), _mbpCycleHandler.GetSmallestSnapshotSequence(),
                                    _mboCycleHandler.GetHighestSnapshotSequence(), _mboCycleHandler.GetSmallestSnapshotSequence());
                            }
                            _lastProcessedSeqNum = _highestSnapshotSequence;
                            _snapshotRecoveryManager.StopRecovery();
                            SwitchState(ChannelState.SYNC);
                            if (Log.IsEnabled(LogLevel.Information))
                            {
                                Log.LogInformation("{Count} Packets added to buffer during initial or outofsync event", _packetsInBufferDuringInitialOrOutOfSync);
                            }
                            _packetsInBufferDuringInitialOrOutOfSync = 0;
                            ProcessMessagesFromBuffer(feedContext);
                            _receivingCycle = false;
                            _numberOfTcpAttempts = 0;
                        }
                    }
                    else
                    {
                        _mboCycleHandler.Reset();
                        _mbpCycleHandler.Reset();
                        _receivingCycle = true;
                    }
                }

                switch (_currentState)
                {
                    case ChannelState.INITIAL:
                    case ChannelState.OUTOFSYNC:
                        if (_receivingCycle)
                        {
                            foreach (IMdpMessage mdpMessage in mdpPacket)
                            {
                                ((IMdpChannelController)this).UpdateSemanticMsgType(_mdpMessageTypes, mdpMessage);
                                long lastMsgSeqNumProcessed = mdpMessage.GetUInt32(MdConstants.LAST_MSG_SEQ_NUM_PROCESSED);
                                int securityId = mdpMessage.GetInt32(MdConstants.SECURITY_ID);
                                long totNumReports = mdpMessage.GetUInt32(MdConstants.TOT_NUM_REPORTS);
                                if (((IMdpChannelController)this).IsMboSnapshot(mdpMessage))
                                {
                                    long noChunks = mdpMessage.GetUInt32(MdConstants.NO_CHUNKS);
                                    long currentChunk = mdpMessage.GetUInt32(MdConstants.CURRENT_CHUNK);
                                    _mboCycleHandler.Update(totNumReports, lastMsgSeqNumProcessed, securityId, noChunks, currentChunk);
                                }
                                else
                                {
                                    _mbpCycleHandler.Update(totNumReports, lastMsgSeqNumProcessed, securityId, 1, 1);
                                }
                            }
                            _target.HandleSnapshotPacket(feedContext, mdpPacket);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void HandleIncrementalPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
        {
            long pkgSequence = mdpPacket.GetMsgSeqNum();
            if (Log.IsEnabled(LogLevel.Trace))
            {
                Log.LogTrace("Feed {FeedType}:{Feed} | HandleIncrementalPacket: previous processed sequence '{Prev}', current packet's sequence '{Curr}'",
                    feedContext.FeedType, feedContext.Feed, _lastProcessedSeqNum, pkgSequence);
            }
            lock (_lock)
            {
                switch (_currentState)
                {
                    case ChannelState.SYNC:
                        long expectedSequence = _lastProcessedSeqNum + 1;
                        if (pkgSequence == expectedSequence)
                        {
                            _target.HandleIncrementalPacket(feedContext, mdpPacket);
                            if (_wasChannelResetInPrcdPacket)
                            {
                                _wasChannelResetInPrcdPacket = false;
                            }
                            else
                            {
                                _lastProcessedSeqNum = pkgSequence;
                            }
                            ProcessMessagesFromBuffer(feedContext);
                        }
                        else if (pkgSequence > expectedSequence)
                        {
                            _buffer.Add(pkgSequence, mdpPacket);
                            if (pkgSequence > (expectedSequence + _gapThreshold))
                            {
                                if (Log.IsEnabled(LogLevel.Information))
                                {
                                    Log.LogInformation("Past gap of {GapThreshold} expected {Expected} current {Current}, lost count {Lost}",
                                        _gapThreshold, expectedSequence, pkgSequence, (pkgSequence - 1) - expectedSequence);
                                }
                                SwitchState(ChannelState.OUTOFSYNC);
                                long amountOfLostMessages = (pkgSequence - 1) - expectedSequence;
                                if (_numberOfTcpAttempts < _maxNumberOfTcpAttempts
                                    && amountOfLostMessages < ITcpMessageRequester.MaxAvailableMessages
                                    && _tcpRecoveryProcessor != null)
                                {
                                    if (Log.IsEnabled(LogLevel.Trace))
                                    {
                                        Log.LogTrace("TCP Replay request gap {Begin}:{End} TCP Attempts: {Attempts}",
                                            expectedSequence, pkgSequence - 1, _numberOfTcpAttempts);
                                    }
                                    _tcpRecoveryProcessor.SetBeginSeqNo(expectedSequence);
                                    _tcpRecoveryProcessor.SetEndSeqNo(pkgSequence - 1);
                                    ThreadPool.QueueUserWorkItem(_ => _tcpRecoveryProcessor.Run());
                                    _numberOfTcpAttempts++;
                                }
                                else
                                {
                                    _snapshotRecoveryManager.StartRecovery();
                                }
                            }
                        }
                        else
                        {
                            if (Log.IsEnabled(LogLevel.Trace))
                            {
                                Log.LogTrace("Feed {FeedType}:{Feed} | HandleIncrementalPacket: packet that has sequence '{Seq}' has been skipped. Expected sequence '{Expected}'",
                                    feedContext.FeedType, feedContext.Feed, pkgSequence, expectedSequence);
                            }
                        }
                        break;

                    case ChannelState.INITIAL:
                    case ChannelState.OUTOFSYNC:
                        _buffer.Add(pkgSequence, mdpPacket);
                        _packetsInBufferDuringInitialOrOutOfSync++;
                        if (Log.IsEnabled(LogLevel.Trace))
                        {
                            Log.LogTrace("Feed {FeedType}:{Feed} | HandleIncrementalPacket: current state is '{State}', so the packet with sequence '{Seq}' has been put into buffer",
                                feedContext.FeedType, feedContext.Feed, _currentState, pkgSequence);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public void PreClose()
        {
            SwitchState(ChannelState.CLOSING);
        }

        public void Close()
        {
            SwitchState(ChannelState.CLOSED);
        }

        public ChannelState GetState() => _currentState;

        /// <summary>
        /// Processes a channel-reset message (Consumer&lt;MdpMessage&gt; accept() in Java).
        /// </summary>
        public void Accept(IMdpMessage resetMessage)
        {
            foreach (IChannelListener channelListener in _channelListeners)
                channelListener.OnBeforeChannelReset(_channelId, resetMessage);

            _lastProcessedSeqNum = 0;
            _smallestSnapshotSequence = 0;
            _highestSnapshotSequence = 0;
            _buffer.Clear();
            _wasChannelResetInPrcdPacket = true;
            if (_currentState != ChannelState.SYNC)
            {
                SwitchState(ChannelState.SYNC);
                _snapshotRecoveryManager.StopRecovery();
            }

            foreach (IChannelListener channelListener in _channelListeners)
                channelListener.OnFinishedChannelReset(_channelId, resetMessage);
        }

        /// <summary>
        /// Manages snapshot recovery lifecycle.
        /// </summary>
        public interface ISnapshotRecoveryManager
        {
            void StartRecovery();
            void StopRecovery();
        }

        private void SwitchState(ChannelState newState)
        {
            Log.LogDebug("Channel '{ChannelId}' has changed its state from '{PrevState}' to '{NewState}'", _channelId, _currentState, newState);
            foreach (IChannelListener listener in _channelListeners)
                listener.OnChannelStateChanged(_channelId, _currentState, newState);
            _currentState = newState;
        }

        private void ProcessMessagesFromBuffer(MdpFeedContext feedContext)
        {
            for (long expectedSequence = _lastProcessedSeqNum + 1; expectedSequence <= _buffer.GetLastMsgSeqNum(); expectedSequence++)
            {
                MdpPacket? mdpPacket = _buffer.Remove(expectedSequence);
                if (mdpPacket != null)
                {
                    _target.HandleIncrementalPacket(feedContext, mdpPacket);
                    _lastProcessedSeqNum = mdpPacket.GetMsgSeqNum();
                }
            }
        }

        private sealed class TcpRecoveryProcessor
        {
            private readonly ITcpMessageRequester _tcpMessageRequester;
            private readonly ITcpPacketListener _tcpPacketListener;
            private long _beginSeqNo;
            private long _endSeqNo;
            private readonly MdpFeedContext _feedContext;
            private readonly GapChannelController _controller;

            public TcpRecoveryProcessor(ITcpMessageRequester tcpMessageRequester,
                ITcpPacketListener tcpPacketListener, GapChannelController controller)
            {
                _tcpMessageRequester = tcpMessageRequester;
                _tcpPacketListener = tcpPacketListener;
                _controller = controller;
                _feedContext = new MdpFeedContext(Feed.A, FeedType.I);
            }

            public void Run()
            {
                try
                {
                    bool result = _tcpMessageRequester.AskForLostMessages(_beginSeqNo, _endSeqNo, _tcpPacketListener);
                    if (result)
                    {
                        lock (_controller._lock)
                        {
                            _controller.SwitchState(ChannelState.SYNC);
                            _controller.ProcessMessagesFromBuffer(_feedContext);
                            _controller._numberOfTcpAttempts = 0;
                        }
                    }
                    else
                    {
                        _controller._snapshotRecoveryManager.StartRecovery();
                    }
                }
                catch (Exception e)
                {
                    Log.LogError(e, "{Message}", e.Message);
                }
            }

            public void SetBeginSeqNo(long beginSeqNo) { _beginSeqNo = beginSeqNo; }
            public void SetEndSeqNo(long endSeqNo) { _endSeqNo = endSeqNo; }
        }

        private sealed class TcpPacketListenerImpl : ITcpPacketListener
        {
            private readonly GapChannelController _controller;

            public TcpPacketListenerImpl(GapChannelController controller) { _controller = controller; }

            public void OnPacket(MdpFeedContext feedContext, MdpPacket mdpPacket)
            {
                _controller.HandleIncrementalPacket(feedContext, mdpPacket);
            }
        }
    }
}
