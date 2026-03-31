using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;
using System.Collections.Generic;

namespace Epam.CmeMdp3Handler.MktData
{
    // Implementation should be complete
    public class TradeHandler : AbstractMktDataHandler, IPublicTrades
    {
        private int _electronicVolume;
        private List<PublicTradeEntity> _publicTrades = new List<PublicTradeEntity>();

        private bool refreshed = false;

        public TradeHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
        }

        public void UpdateTradeSummary(IFieldSet tradeEntry)
        {
            var publicTradeEntity = new PublicTradeEntity();
            publicTradeEntity.RefreshBookFromMessage(tradeEntry);
            _publicTrades.Add(publicTradeEntity);
            refreshed = true;
        }

        public void UpdateElectronicVolume(IFieldSet incrementEntry)
        {
            _electronicVolume = incrementEntry.GetInt32(271);
            refreshed = true;
        }

        public override void Clear()
        {
            _electronicVolume = 0;
            _publicTrades = new List<PublicTradeEntity>();
            refreshed = false;
        }

        public int ElectronicVolume() => _electronicVolume;

        public List<PublicTradeEntity> PublicTrades()
        {
            var publicTrades = _publicTrades;
            _publicTrades = new List<PublicTradeEntity>();
            return publicTrades;
        }

        public void CommitEvent()
        {
            if (refreshed) channelContext.NotifyTradeSummary(this);
            refreshed = false;
        }
    }
}
