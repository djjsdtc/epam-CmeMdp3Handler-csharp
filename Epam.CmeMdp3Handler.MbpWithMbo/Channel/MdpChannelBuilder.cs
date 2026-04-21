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
using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Core.Channel.Tcp;
using Epam.CmeMdp3Handler.MbpWithMbo.Control;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Epam.CmeMdp3Handler.Service;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Channel
{
    /// <summary>
    /// Fluent builder for <see cref="IMdpChannel"/> instances in MBP-with-MBO mode.
    ///
    /// Java: com.epam.cme.mdp3.channel.MdpChannelBuilder
    /// C# note: Java uses URI -> C# uses System.Uri.
    ///          Java ScheduledExecutorService -> C# DefaultScheduledServiceHolder.SchedulerHolder.
    /// </summary>
    public class MdpChannelBuilder
    {
        public const int DefIncrQueueSize = 15000;
        public const int DefGapThreshold = 5;

        private readonly string _channelId;
        private Uri? _cfgUri;
        private Uri? _schemaUri;

        // Per-feedType per-feed network interfaces
        private readonly Dictionary<FeedType, string?> _feedANetworkInterfaces = new();
        private readonly Dictionary<FeedType, string?> _feedBNetworkInterfaces = new();

        private IChannelListener? _channelListener;
        private DefaultScheduledServiceHolder.SchedulerHolder? _scheduler;
        private int _incrQueueSize = DefIncrQueueSize;
        private int _gapThreshold = DefGapThreshold;
        private int _rcvBufSize = MdpFeedWorker.RCV_BUFFER_SIZE;
        private string _tcpUsername = MdpTcpMessageRequester.DefaultUsername;
        private string _tcpPassword = MdpTcpMessageRequester.DefaultPassword;
        private int _maxNumberOfTcpAttempts = GapChannelController.MaxNumberOfTcpAttempts;
        private bool _mboEnabled = false;
        private IList<int>? _incrementMessageTemplateIds;
        private IList<int>? _snapshotMessageTemplateIds;
        private Feed _snptFeedToUse = Feed.A;
        private int _outputStatisticsEveryXseconds = 0;

        public MdpChannelBuilder(string channelId)
        {
            _channelId = channelId;
        }

        public MdpChannelBuilder(string channelId, Uri cfgUri, Uri schemaUri)
        {
            _channelId = channelId;
            _cfgUri = cfgUri;
            SetSchema(schemaUri);
        }

        public MdpChannelBuilder SetConfiguration(Uri cfgUri)
        {
            _cfgUri = cfgUri;
            return this;
        }

        public MdpChannelBuilder SetSchema(Uri schemaUri)
        {
            _schemaUri = schemaUri;
            return this;
        }

        /// <summary>
        /// Sets the local network interface for a specific feed type and feed side.
        /// If set to null the default local network interface is used.
        /// </summary>
        public MdpChannelBuilder SetNetworkInterface(FeedType feedType, Feed feed, string? networkInterface)
        {
            if (feed == Feed.A) _feedANetworkInterfaces[feedType] = networkInterface;
            else if (feed == Feed.B) _feedBNetworkInterfaces[feedType] = networkInterface;
            return this;
        }

        /// <summary>Sets the channel listener.</summary>
        public MdpChannelBuilder UsingListener(IChannelListener channelListener)
        {
            _channelListener = channelListener;
            return this;
        }

        /// <summary>
        /// Sets the scheduler used for TCP recovery tasks and idle-feed checking.
        /// Pass null to disable scheduled tasks.
        /// </summary>
        public MdpChannelBuilder UsingScheduler(DefaultScheduledServiceHolder.SchedulerHolder? scheduler)
        {
            _scheduler = scheduler;
            return this;
        }

        /// <summary>
        /// Sets the size of the queue used for buffering incremental messages during recovery.
        /// Default: 15000.
        /// </summary>
        public MdpChannelBuilder UsingIncrQueueSize(int incrQueueSize)
        {
            _incrQueueSize = incrQueueSize;
            return this;
        }

        /// <summary>
        /// Sets the number of lost messages after which recovery procedure starts.
        /// Default: 5.
        /// </summary>
        public MdpChannelBuilder UsingGapThreshold(int gapThreshold)
        {
            _gapThreshold = gapThreshold;
            return this;
        }

        /// <summary>Sets the UDP socket buffer size. Default: 4 MB.</summary>
        public MdpChannelBuilder UsingRcvBufSize(int rcvBufSize)
        {
            _rcvBufSize = rcvBufSize;
            return this;
        }

        /// <summary>Sets the maximum number of TCP recovery attempts before falling back to snapshot.</summary>
        public MdpChannelBuilder UsingMaxNumberOfTcpAttempts(int maxTcpAttempts)
        {
            _maxNumberOfTcpAttempts = maxTcpAttempts;
            return this;
        }

        /// <summary>Sets the username for the TCP replay feed logon.</summary>
        public MdpChannelBuilder SetTcpUsername(string tcpUsername)
        {
            _tcpUsername = tcpUsername;
            return this;
        }

        /// <summary>Sets the password for the TCP replay feed logon.</summary>
        public MdpChannelBuilder SetTcpPassword(string tcpPassword)
        {
            _tcpPassword = tcpPassword;
            return this;
        }

        /// <summary>
        /// Enables MBO mode in which order entries in the messages are processed
        /// and the MBO snapshot feed is used. Default: false (disabled).
        /// </summary>
        public MdpChannelBuilder SetMboEnable(bool enabled)
        {
            _mboEnabled = enabled;
            return this;
        }

        /// <summary>Sets custom MBO incremental message template IDs (overrides defaults 43 and 47).</summary>
        public MdpChannelBuilder SetIncrementMessageTemplateIds(IList<int> ids)
        {
            _incrementMessageTemplateIds = ids;
            return this;
        }

        /// <summary>Sets custom MBO snapshot message template IDs (overrides defaults 44 and 53).</summary>
        public MdpChannelBuilder SetSnapshotMessageTemplateIds(IList<int> ids)
        {
            _snapshotMessageTemplateIds = ids;
            return this;
        }

        /// <summary>Sets which snapshot feed (A or B) to use during recovery. Default: A.</summary>
        public MdpChannelBuilder SetSnapshotFeedToUse(Feed snptFeedToUse)
        {
            _snptFeedToUse = snptFeedToUse;
            return this;
        }

        /// <summary>
        /// Enables periodic statistics logging every X seconds. 0 disables (default).
        /// </summary>
        public MdpChannelBuilder SetOutputStatisticsEveryXseconds(int outputStatisticsEveryXseconds)
        {
            _outputStatisticsEveryXseconds = outputStatisticsEveryXseconds;
            return this;
        }

        /// <summary>Builds the MDP channel with the configured parameters.</summary>
        public IMdpChannel Build()
        {
            try
            {
                var cfg = new Configuration(_cfgUri!);
                var mdpMessageTypes = new MdpMessageTypes(_schemaUri!);

                var scheduler = _scheduler ?? DefaultScheduledServiceHolder.GetScheduler();

                var mdpChannel = new LowLevelMdpChannel(
                    scheduler,
                    cfg.GetChannel(_channelId)!,
                    mdpMessageTypes,
                    _incrQueueSize,
                    _rcvBufSize,
                    _gapThreshold,
                    _maxNumberOfTcpAttempts,
                    _tcpUsername,
                    _tcpPassword,
                    _feedANetworkInterfaces,
                    _feedBNetworkInterfaces,
                    _mboEnabled,
                    _incrementMessageTemplateIds,
                    _snapshotMessageTemplateIds,
                    _snptFeedToUse,
                    _outputStatisticsEveryXseconds);

                if (_channelListener != null) mdpChannel.RegisterListener(_channelListener);
                return mdpChannel;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to build MDP Channel", e);
            }
        }
    }
}
