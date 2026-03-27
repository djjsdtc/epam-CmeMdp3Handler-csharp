using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class ImpliedBookPriceEntry : IImpliedBookPriceLevel
    {
        protected int qty;
        protected readonly Price price = new Price();

        public int GetQuantity() => qty;
        public Price GetPrice() => price;

        public void Clear()
        {
            qty = 0;
            price.SetNull();
        }

        public void RefreshFromAnotherEntry(ImpliedBookPriceEntry bookEntry)
        {
            this.qty = bookEntry.qty;
            this.price.SetMantissa(bookEntry.GetPrice().GetMantissa());
        }

        public void RefreshFromMessage(IFieldSet fieldSet)
        {
            this.qty = fieldSet.GetInt32(271);
            this.price.SetMantissa(fieldSet.GetInt64(270));
        }
    }
}
