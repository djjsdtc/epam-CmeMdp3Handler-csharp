using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public class SecurityStatusHandler
    {
        private readonly ChannelContext _channelContext;
        private readonly SbeString _secGroup = SbeString.Allocate(10);
        private readonly SbeString _secAsset = SbeString.Allocate(10);

        public SecurityStatusHandler(ChannelContext channelContext)
        {
            _channelContext = channelContext;
        }

        public void Handle(IMdpMessage statusMessage, short matchEventIndicator)
        {
            int securityId = statusMessage.GetInt32(48);
            _channelContext.NotifySecurityStatus(securityId, statusMessage);

            if (_channelContext.HasMdListeners())
            {
                statusMessage.GetString(1151, _secGroup);
                statusMessage.GetString(6937, _secAsset);
                int tradeDate = statusMessage.GetUInt16(75);
                SecurityTradingStatus? secTrdStatus = SecurityTradingStatusExtensions.FromFIX(statusMessage.GetUInt8(326));
                HaltReason? haltReason = HaltReasonExtensions.FromFIX(statusMessage.GetUInt8(327));
                SecurityTradingEvent secTrdEvent = SecurityTradingEventExtensions.FromFIX(statusMessage.GetInt8(1174));

                _channelContext.NotifySecurityStatus(_secGroup, _secAsset, securityId, tradeDate,
                    matchEventIndicator, secTrdStatus, haltReason, secTrdEvent);
            }
        }
    }
}
