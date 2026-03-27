using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class MultipleDepthBookHandler : AbstractOrderBookHandler<OrderBookPriceEntry>, IOrderBook
    {
        private bool _subscribedToTop = false;
        private bool _subscribedToEntireBook = false;
        private readonly OrderBookPriceEntry[] _bidLevels;
        private readonly OrderBookPriceEntry[] _offerLevels;
        private readonly byte _depth;

        public MultipleDepthBookHandler(ChannelContext channelContext, int securityId, int subscriptionFlags, byte depth)
            : base(channelContext, securityId, subscriptionFlags)
        {
            SetSubscriptionFlags(subscriptionFlags);
            this._depth = depth;
            _bidLevels = new OrderBookPriceEntry[depth];
            _offerLevels = new OrderBookPriceEntry[depth];
            Init();
        }

        private void Init()
        {
            for (int i = 0; i < _depth; i++)
            {
                _bidLevels[i] = new OrderBookPriceEntry();
                _offerLevels[i] = new OrderBookPriceEntry();
            }
        }

        public override void Clear()
        {
            for (int i = 0; i < _depth; i++)
            {
                _bidLevels[i].Clear();
                _offerLevels[i].Clear();
            }
            refreshedTop = false;
            refreshedBook = false;
        }

        public override void SetSubscriptionFlags(int subscriptionFlags)
        {
            base.SetSubscriptionFlags(subscriptionFlags);
            _subscribedToEntireBook = MdEventFlags.HasBook(this.subscriptionFlags);
            _subscribedToTop = MdEventFlags.HasTop(this.subscriptionFlags);
        }

        public void HandleSnapshotBidEntry(IMdpGroup snptGroup)
        {
            byte level = snptGroup.GetInt8(1023);
            if (level > 1 && (!_subscribedToEntireBook || level > _depth)) return;
            ((OrderBookPriceEntry)GetBid(level)).RefreshBookFromMessage(snptGroup);
        }

        public void HandleSnapshotOfferEntry(IMdpGroup snptGroup)
        {
            byte level = snptGroup.GetInt8(1023);
            if (level > 1 && (!_subscribedToEntireBook || level > _depth)) return;
            ((OrderBookPriceEntry)GetOffer(level)).RefreshBookFromMessage(snptGroup);
        }

        public void HandleIncrementBidEntry(IFieldSet incrementEntry)
        {
            byte level = (byte)incrementEntry.GetUInt8(1023);
            if (level > 1 && (!_subscribedToEntireBook || level > _depth)) return;
            refreshedBook = true;
            if (level == 1) refreshedTop = true;
            var updateAction = MDUpdateActionExtensions.FromFIX(incrementEntry.GetUInt8(279));
            HandleIncrementRefresh(_bidLevels, level, updateAction, incrementEntry);
        }

        public void HandleIncrementOfferEntry(IFieldSet incrementEntry)
        {
            byte level = (byte)incrementEntry.GetUInt8(1023);
            if (level > 1 && (!_subscribedToEntireBook || level > _depth)) return;
            refreshedBook = true;
            if (level == 1) refreshedTop = true;
            var updateAction = MDUpdateActionExtensions.FromFIX(incrementEntry.GetUInt8(279));
            HandleIncrementRefresh(_offerLevels, level, updateAction, incrementEntry);
        }

        protected override void DeleteEntry(OrderBookPriceEntry[] levelEntries, int level)
        {
            for (int i = level - 1; i < _depth - 1; i++)
                levelEntries[i].RefreshFromAnotherEntry(levelEntries[i + 1]);
            levelEntries[_depth - 1].Clear();
        }

        protected override void DeleteFrom(OrderBookPriceEntry[] levelEntries, int n)
        {
            if (n >= _depth) { DeleteThru(levelEntries); return; }
            for (int i = n; i < _depth; i++)
                levelEntries[i - n].RefreshFromAnotherEntry(levelEntries[i]);
            for (int i = _depth - n; i < _depth; i++)
                levelEntries[i].Clear();
        }

        protected override void DeleteThru(OrderBookPriceEntry[] levelEntries)
        {
            for (int i = 0; i < _depth; i++)
                levelEntries[i].Clear();
        }

        protected override void InsertEntry(OrderBookPriceEntry[] levelEntries, int level, IFieldSet fieldSet)
        {
            for (int i = _depth - 1; i > level - 1; i--)
                levelEntries[i].RefreshFromAnotherEntry(levelEntries[i - 1]);
            levelEntries[level - 1].RefreshBookFromMessage(fieldSet);
        }

        protected override void ModifyEntry(OrderBookPriceEntry[] levelEntries, int level, IFieldSet fieldSet)
        {
            levelEntries[level - 1].RefreshBookFromMessage(fieldSet);
        }

        public byte GetDepth() => _depth;

        public IOrderBookPriceLevel GetBid(byte level) => _bidLevels[level - 1];
        public IOrderBookPriceLevel GetOffer(byte level) => _offerLevels[level - 1];

        public void CommitEvent()
        {
            if (_subscribedToTop && refreshedTop) channelContext.NotifyTopOfBookRefresh(this);
            if (_subscribedToEntireBook && refreshedBook) channelContext.NotifyBookRefresh(this);
            refreshedTop = false;
            refreshedBook = false;
        }
    }
}
