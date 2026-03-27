using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public interface IOrderBookPriceLevel : IImpliedBookPriceLevel
    {
        int GetOrderCount();
        void RefreshBookFromMessage(IFieldSet fieldSet);
    }
}
