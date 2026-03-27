using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.Core.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;
using Microsoft.Extensions.Logging;

namespace Epam.CmeMdp3Handler.MktData
{
    public class RequestForQuoteHandler
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RequestForQuoteHandler>();

        private readonly SbeString _quoReqId = SbeString.Allocate(40);
        private readonly ChannelContext _channelContext;

        public RequestForQuoteHandler(ChannelContext channelContext)
        {
            _channelContext = channelContext;
        }

        public void Handle(MdpFeedContext feedContext, IMdpMessage quoReqMessage)
        {
            _channelContext.NotifyRequestForQuote(quoReqMessage);

            if (_channelContext.HasMdListeners())
            {
                var quoReqGroup = feedContext.GetMdpGroupObj();

                quoReqMessage.GetString(131, _quoReqId);
                quoReqMessage.GetGroup(146, quoReqGroup);

                while (quoReqGroup.HasNext())
                {
                    quoReqGroup.Next();
                    int secId = quoReqGroup.GetInt32(48);
                    int orderQty = quoReqGroup.GetInt32(38);
                    Side? side = SideExtensions.FromFIX(quoReqGroup.GetInt8(54));
                    _channelContext.NotifyRequestForQuote(_quoReqId, quoReqGroup.GetEntryNum(),
                        quoReqGroup.GetNumInGroup(), secId, QuoteType.Tradable, orderQty, side);
                }
            }
        }
    }
}
