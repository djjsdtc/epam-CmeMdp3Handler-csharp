using Epam.CmeMdp3Handler.Core.Cfg;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.Core.Channel
{
    public class MdpFeedContext
    {
        public Feed     Feed     { get; }
        public FeedType FeedType { get; }

        private readonly MdpPacket      _mdpPacket        = MdpPacket.Allocate();
        private readonly IMdpGroup      _mdpGroupObj      = SbeGroup.Instance();
        private readonly IMdpGroupEntry _mdpGroupEntryObj = SbeGroupEntry.Instance();
        private readonly SbeDouble      _doubleValObj     = SbeDouble.Instance();
        private readonly SbeString      _strValObj        = SbeString.Allocate(256);

        public MdpFeedContext(Feed feed, FeedType feedType)
        {
            Feed     = feed;
            FeedType = feedType;
        }

        public MdpFeedContext(ConnectionCfg cfg)
        {
            Feed     = cfg.Feed;
            FeedType = cfg.FeedType;
        }

        public MdpPacket      GetMdpPacket()        => _mdpPacket;
        public IMdpGroup      GetMdpGroupObj()      => _mdpGroupObj;
        public IMdpGroupEntry GetMdpGroupEntryObj() => _mdpGroupEntryObj;
        public SbeDouble      GetDoubleValObj()     => _doubleValObj;
        public SbeString      GetStrValObj()        => _strValObj;
    }
}
