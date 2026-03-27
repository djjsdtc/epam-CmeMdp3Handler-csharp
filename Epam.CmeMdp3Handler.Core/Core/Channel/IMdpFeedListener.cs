namespace Epam.CmeMdp3Handler.Core.Channel
{
    public interface IMdpFeedListener
    {
        void OnFeedStarted(FeedType feedType, Feed feed);
        void OnFeedStopped(FeedType feedType, Feed feed);
        void OnPacket(MdpFeedContext feedContext, MdpPacket mdpPacket);
    }
}
