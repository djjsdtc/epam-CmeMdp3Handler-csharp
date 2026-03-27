using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class OrderBookPriceEntry : ImpliedBookPriceEntry, IOrderBookPriceLevel
    {
        private int _orderCount;

        public int GetOrderCount() => _orderCount;

        public new void Clear()
        {
            base.Clear();
            _orderCount = 0;
        }

        public void RefreshFromAnotherEntry(OrderBookPriceEntry bookEntry)
        {
            base.RefreshFromAnotherEntry(bookEntry);
            this._orderCount = bookEntry.GetOrderCount();
        }

        public void RefreshBookFromMessage(IFieldSet fieldSet)
        {
            base.RefreshFromMessage(fieldSet);
            this._orderCount = fieldSet.GetInt32(346);
        }
    }
}
