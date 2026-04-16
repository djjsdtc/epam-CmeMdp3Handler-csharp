using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class ImpliedBookHandler : AbstractOrderBookHandler<ImpliedBookPriceEntry>, IImpliedBook
    {
        private const int Depth = IImpliedBook.PlatformImpliedBookDepth;
        private bool _subscribedToTop = false;
        private bool _subscribedToEntireBook = false;
        private readonly ImpliedBookPriceEntry[] _bidLevels;
        private readonly ImpliedBookPriceEntry[] _offerLevels;

        public ImpliedBookHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
            SetSubscriptionFlags(subscriptionFlags);
            _bidLevels = new ImpliedBookPriceEntry[Depth];
            _offerLevels = new ImpliedBookPriceEntry[Depth];
            Init();
        }

        private void Init()
        {
            for (int i = 0; i < Depth; i++)
            {
                _bidLevels[i] = new OrderBookPriceEntry();
                _offerLevels[i] = new OrderBookPriceEntry();
            }
        }

        public override void Clear()
        {
            for (int i = 0; i < Depth; i++)
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
            _subscribedToEntireBook = MdEventFlags.HasImpliedBook(this.subscriptionFlags);
            _subscribedToTop = MdEventFlags.HasImpliedTop(this.subscriptionFlags);
        }

        public void HandleSnapshotBidEntry(IMdpGroup snptGroup)
        {
            byte level = (byte)snptGroup.GetInt8(1023);
            if (level > 1 && !_subscribedToEntireBook) return;
            ((ImpliedBookPriceEntry)GetBid(level)).RefreshFromMessage(snptGroup);
        }

        public void HandleSnapshotOfferEntry(IMdpGroup snptGroup)
        {
            byte level = (byte)snptGroup.GetInt8(1023);
            if (level > 1 && !_subscribedToEntireBook) return;
            ((ImpliedBookPriceEntry)GetOffer(level)).RefreshFromMessage(snptGroup);
        }

        public void HandleIncrementBidEntry(IFieldSet incrementEntry)
        {
            byte level = (byte)incrementEntry.GetUInt8(1023);
            if (level > 1 && !_subscribedToEntireBook) return;
            refreshedBook = true;
            if (level == 1) refreshedTop = true;
            var updateAction = MDUpdateActionExtensions.FromFIX(incrementEntry.GetUInt8(279));
            HandleIncrementRefresh(_bidLevels, level, updateAction, incrementEntry);
        }

        public void HandleIncrementOfferEntry(IFieldSet incrementEntry)
        {
            byte level = (byte)incrementEntry.GetUInt8(1023);
            if (level > 1 && !_subscribedToEntireBook) return;
            refreshedBook = true;
            if (level == 1) refreshedTop = true;
            var updateAction = MDUpdateActionExtensions.FromFIX(incrementEntry.GetUInt8(279));
            HandleIncrementRefresh(_offerLevels, level, updateAction, incrementEntry);
        }

        protected override void DeleteEntry(ImpliedBookPriceEntry[] levelEntries, int level)
        {
            for (int i = level - 1; i < Depth - 1; i++)
                levelEntries[i].RefreshFromAnotherEntry(levelEntries[i + 1]);
            levelEntries[Depth - 1].Clear();
        }

        protected override void DeleteFrom(ImpliedBookPriceEntry[] levelEntries, int n)
        {
            if (n >= Depth) { DeleteThru(levelEntries); return; }
            for (int i = n; i < Depth; i++)
                levelEntries[i - n].RefreshFromAnotherEntry(levelEntries[i]);
            for (int i = Depth - n; i < Depth; i++)
                levelEntries[i].Clear();
        }

        protected override void DeleteThru(ImpliedBookPriceEntry[] levelEntries)
        {
            for (int i = 0; i < Depth; i++)
                levelEntries[i].Clear();
        }

        protected override void InsertEntry(ImpliedBookPriceEntry[] levelEntries, int level, IFieldSet fieldSet)
        {
            for (int i = Depth - 1; i > level - 1; i--)
                levelEntries[i].RefreshFromAnotherEntry(levelEntries[i - 1]);
            levelEntries[level - 1].RefreshFromMessage(fieldSet);
        }

        protected override void ModifyEntry(ImpliedBookPriceEntry[] levelEntries, int level, IFieldSet fieldSet)
        {
            levelEntries[level - 1].RefreshFromMessage(fieldSet);
        }

        public bool IsSubscribedToTop() => _subscribedToTop;
        public bool IsSubscribedToEntireBook() => _subscribedToEntireBook;

        public IImpliedBookPriceLevel GetBid(byte level) => _bidLevels[level - 1];
        public IImpliedBookPriceLevel GetOffer(byte level) => _offerLevels[level - 1];

        public void CommitEvent()
        {
            if (_subscribedToTop && refreshedTop) channelContext.NotifyImpliedTopOfBookRefresh(this);
            if (_subscribedToEntireBook && refreshedBook) channelContext.NotifyImpliedBookRefresh(this);
            refreshedTop = false;
            refreshedBook = false;
        }
    }
}
