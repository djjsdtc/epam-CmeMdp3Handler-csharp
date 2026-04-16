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
        private List<PublicTradeEntity> _newTrades = new List<PublicTradeEntity>();
        private List<PublicTradeEntity> _adjustTrades = new List<PublicTradeEntity>();

        private bool refreshed = false;

        public TradeHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
        }

        public void UpdateTradeSummary(IFieldSet tradeEntry)
        {
            var publicTradeEntity = new PublicTradeEntity();
            publicTradeEntity.RefreshBookFromMessage(tradeEntry);
            if (publicTradeEntity.GetAction() == MDUpdateAction.New)
                _newTrades.Add(publicTradeEntity);
            else if (publicTradeEntity.GetAction() == MDUpdateAction.Change || publicTradeEntity.GetAction() == MDUpdateAction.Delete)
                _adjustTrades.Add(publicTradeEntity);
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
            _newTrades = new List<PublicTradeEntity>();
            _adjustTrades = new List<PublicTradeEntity>();
            refreshed = false;
        }

        public int ElectronicVolume() => _electronicVolume;

        public List<PublicTradeEntity> NewTrades()
        {
            var newTrades = _newTrades;
            _newTrades = new List<PublicTradeEntity>();
            return newTrades;
        }

        public List<PublicTradeEntity> AdjustTrades()
        {
            var adjustTrades = _adjustTrades;
            _adjustTrades = new List<PublicTradeEntity>();
            return adjustTrades;
        }

        public void CommitEvent()
        {
            if (refreshed) channelContext.NotifyTradeSummary(this);
            refreshed = false;
        }
    }
}
