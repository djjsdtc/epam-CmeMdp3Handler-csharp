namespace Epam.CmeMdp3Handler.MktData
{
    using Epam.CmeMdp3Handler.MktData.Enums;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PublicTradeEntity
    {
        private readonly Price tradePrice = new Price();
        private int quantity;
        private int numberOfOrders;
        private Side? aggressorSide;
        private uint tradeId;

        public void Clear()
        {
            tradePrice.SetNull();
            quantity = 0;
            numberOfOrders = 0;
            aggressorSide = null;
            tradeId = 0;
        }

        public Price GetTradePrice() => tradePrice;

        public int GetQuantity() => quantity;

        public int GetNumberOfOrders() => numberOfOrders;

        public Side? GetAggressorSide() => aggressorSide;

        public uint GetTradeId() => tradeId;

        public void RefreshFromAnotherEntry(PublicTradeEntity bookEntry)
        {
            this.tradePrice.SetMantissa(bookEntry.GetTradePrice().GetMantissa());
            this.quantity = bookEntry.GetQuantity();
            this.numberOfOrders = bookEntry.GetNumberOfOrders();
            this.aggressorSide = bookEntry.GetAggressorSide();
            this.tradeId = bookEntry.GetTradeId();
        }

        public void RefreshBookFromMessage(IFieldSet fieldSet)
        {
            this.tradePrice.SetMantissa(fieldSet.GetInt64(270));
            this.quantity = fieldSet.GetInt32(271);
            this.numberOfOrders = fieldSet.GetInt32(346);
            this.aggressorSide = SideExtensions.FromFIX((byte)fieldSet.GetInt32(5797));
            this.tradeId = (uint)fieldSet.GetUInt32(37711);
        }
    }
}
