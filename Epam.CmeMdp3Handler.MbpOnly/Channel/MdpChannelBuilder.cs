using System;
using Epam.CmeMdp3Handler.Control;
using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.Sbe.Schema;
using Epam.CmeMdp3Handler.Service;

namespace Epam.CmeMdp3Handler.Channel
{
    public class MdpChannelBuilder
    {
        private readonly string _channelId;
        private Uri? _cfgUri;
        private Uri? _schemaUri;
        private MdpMessageTypes? _mdpMessageTypes;

        private string? _incrementalFeedAni;
        private string? _incrementalFeedBni;
        private string? _snapshotFeedAni;
        private string? _snapshotFeedBni;
        private string? _instrumentFeedAni;
        private string? _instrumentFeedBni;

        private IChannelListener? _channelListener;
        private bool _noScheduler = false;
        private DefaultScheduledServiceHolder.SchedulerHolder? _scheduler;

        private int _queueSlotInitBufferSize = InstrumentController.DefQueueSlotInitBufferSize;
        private int _incrQueueSize = InstrumentController.DefIncrQueueSize;
        private int _gapThreshold = InstrumentController.DefGapThreshold;
        private int _rcvBufSize = MdpFeedWorker.RCV_BUFFER_SIZE;

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
            _mdpMessageTypes = new MdpMessageTypes(schemaUri);
            return this;
        }

        public MdpChannelBuilder SetNetworkInterface(FeedType feedType, Feed feed, string networkInterface)
        {
            if (feedType == FeedType.I)
            {
                if (feed == Feed.A) _incrementalFeedAni = networkInterface;
                else if (feed == Feed.B) _incrementalFeedBni = networkInterface;
            }
            else if (feedType == FeedType.S)
            {
                if (feed == Feed.A) _snapshotFeedAni = networkInterface;
                else if (feed == Feed.B) _snapshotFeedBni = networkInterface;
            }
            else if (feedType == FeedType.N)
            {
                if (feed == Feed.A) _instrumentFeedAni = networkInterface;
                else if (feed == Feed.B) _instrumentFeedBni = networkInterface;
            }
            return this;
        }

        public MdpChannelBuilder UsingListener(IChannelListener channelListener)
        {
            _channelListener = channelListener;
            return this;
        }

        public MdpChannelBuilder UsingScheduler(DefaultScheduledServiceHolder.SchedulerHolder scheduler)
        {
            _scheduler = scheduler;
            return this;
        }

        public MdpChannelBuilder UsingQueueSlotInitBufferSize(int size)
        {
            _queueSlotInitBufferSize = size;
            return this;
        }

        public MdpChannelBuilder UsingIncrQueueSize(int size)
        {
            _incrQueueSize = size;
            return this;
        }

        public MdpChannelBuilder UsingGapThreshold(int gapThreshold)
        {
            _gapThreshold = gapThreshold;
            return this;
        }

        public MdpChannelBuilder UsingRcvBufSize(int rcvBufSize)
        {
            _rcvBufSize = rcvBufSize;
            return this;
        }

        public MdpChannelBuilder NoFeedIdleControl()
        {
            _noScheduler = true;
            return this;
        }

        public IMdpChannel Build()
        {
            try
            {
                var cfg = new Configuration(_cfgUri!);
                var mdpMessageTypes = new MdpMessageTypes(_schemaUri!);

                DefaultScheduledServiceHolder.SchedulerHolder? scheduler = null;
                if (!_noScheduler)
                    scheduler = _scheduler ?? DefaultScheduledServiceHolder.GetScheduler();

                var mdpChannel = new MdpChannelImpl(scheduler, cfg.GetChannel(_channelId)!, mdpMessageTypes,
                    _queueSlotInitBufferSize, _incrQueueSize, _gapThreshold);

                mdpChannel.SetIncrementalFeedAni(_incrementalFeedAni);
                mdpChannel.SetIncrementalFeedBni(_incrementalFeedBni);
                mdpChannel.SetSnapshotFeedAni(_snapshotFeedAni);
                mdpChannel.SetSnapshotFeedBni(_snapshotFeedBni);
                mdpChannel.SetInstrumentFeedAni(_instrumentFeedAni);
                mdpChannel.SetInstrumentFeedBni(_instrumentFeedBni);
                mdpChannel.SetRcvBufSize(_rcvBufSize);

                if (_channelListener != null) mdpChannel.RegisterListener(_channelListener);
                return mdpChannel;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to build MDP Channel", e);
            }
        }

        public MdpMessageTypes? GetMdpMessageTypes() => _mdpMessageTypes;
    }
}
