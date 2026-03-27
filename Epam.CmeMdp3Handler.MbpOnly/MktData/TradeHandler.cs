using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation should be complete
    public class TradeHandler : AbstractMktDataHandler
    {
        public TradeHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
        }

        public void UpdateTradeSummary(IFieldSet tradeEntry)
        {
            throw new System.NotSupportedException();
        }

        public void UpdateElectronicVolume(IFieldSet incrementEntry)
        {
            throw new System.NotSupportedException();
        }

        public override void Clear()
        {
            throw new System.NotSupportedException();
        }
    }
}
