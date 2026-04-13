using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class ImpliedBookPriceEntry : IImpliedBookPriceLevel
    {
        protected int qty;
        protected SbeDouble price = SbeDouble.NullInstance();

        public int GetQuantity() => qty;
        public double? GetPrice() => price.AsNullableDouble();

        public void Clear()
        {
            qty = 0;
            price.Reset();
            price.SetNull(true);
        }

        public void RefreshFromAnotherEntry(ImpliedBookPriceEntry bookEntry)
        {
            this.qty = bookEntry.qty;
            this.price = bookEntry.price;
        }

        public void RefreshFromMessage(IFieldSet fieldSet)
        {
            this.qty = fieldSet.GetInt32(271);
            this.price.SetMantissa(fieldSet.GetInt64(270));
        }
    }
}
