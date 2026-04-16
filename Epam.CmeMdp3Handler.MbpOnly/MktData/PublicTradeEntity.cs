namespace Epam.CmeMdp3Handler.MktData
{
    using Epam.CmeMdp3Handler.MktData.Enums;
    using Epam.CmeMdp3Handler.Sbe.Message;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class PublicTradeEntity
    {
        private readonly SbeDouble tradePrice = SbeDouble.NullInstance();
        private int quantity;
        private int numberOfOrders;
        private Side? aggressorSide;
        private uint tradeId;
        private MDUpdateAction? action;

        public void Clear()
        {
            tradePrice.Reset(true);
            quantity = 0;
            numberOfOrders = 0;
            aggressorSide = null;
            tradeId = 0;
            action = null;
        }

        public MDUpdateAction? GetAction() => action;

        public double? GetTradePrice() => tradePrice.AsNullableDouble();

        public int GetQuantity() => quantity;

        public int GetNumberOfOrders() => numberOfOrders;

        public Side? GetAggressorSide() => aggressorSide;

        public uint GetTradeId() => tradeId;

        public void RefreshFromAnotherEntry(PublicTradeEntity bookEntry)
        {
            this.action = bookEntry.GetAction();
            this.tradePrice.SetMantissa(bookEntry.tradePrice.GetMantissa());
            this.tradePrice.SetExponent(bookEntry.tradePrice.GetExponent());
            this.tradePrice.SetNull(bookEntry.tradePrice.IsNull());
            this.quantity = bookEntry.GetQuantity();
            this.numberOfOrders = bookEntry.GetNumberOfOrders();
            this.aggressorSide = bookEntry.GetAggressorSide();
            this.tradeId = bookEntry.GetTradeId();
        }

        public void RefreshBookFromMessage(IFieldSet fieldSet)
        {
            this.action = MDUpdateActionExtensions.FromFIX(fieldSet.GetUInt8(279));
            fieldSet.GetDouble(270, this.tradePrice);
            this.quantity = fieldSet.GetInt32(271);
            this.numberOfOrders = fieldSet.GetInt32(346);
            this.aggressorSide = SideExtensions.FromFIX((sbyte)fieldSet.GetUInt8(5797));
            this.tradeId = (uint)fieldSet.GetUInt32(37711);
        }
    }
}
