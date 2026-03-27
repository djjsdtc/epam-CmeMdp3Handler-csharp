using Epam.CmeMdp3Handler.Channel;

namespace Epam.CmeMdp3Handler.MktData
{
    public abstract class AbstractMktDataHandler
    {
        protected ChannelContext channelContext;
        protected int securityId;
        protected int subscriptionFlags;
        private readonly object _lock = new object();

        protected AbstractMktDataHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
        {
            this.channelContext = channelContext;
            this.securityId = securityId;
            this.subscriptionFlags = subscriptionFlags;
        }

        public abstract void Clear();

        public void Lock() => System.Threading.Monitor.Enter(_lock);
        public void Unlock() => System.Threading.Monitor.Exit(_lock);

        public int GetSecurityId() => securityId;
        public int GetSubscriptionFlags() => subscriptionFlags;

        public virtual void SetSubscriptionFlags(int subscriptionFlags)
        {
            this.subscriptionFlags = subscriptionFlags;
        }
    }
}
