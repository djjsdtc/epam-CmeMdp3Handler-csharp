using Epam.CmeMdp3Handler.Channel;
using Epam.CmeMdp3Handler.MktData.Enums;
using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.MktData
{
    public abstract class AbstractOrderBookHandler<T> : AbstractMktDataHandler
    {
        protected bool refreshedTop = false;
        protected bool refreshedBook = false;

        protected AbstractOrderBookHandler(ChannelContext channelContext, int securityId, int subscriptionFlags)
            : base(channelContext, securityId, subscriptionFlags)
        {
        }

        public override abstract void Clear();

        public bool IsRefreshedTop() => refreshedTop;
        public void LogRefreshedTop() => refreshedTop = true;
        public void ResetTransaction() => refreshedTop = false;

        protected abstract void ModifyEntry(T[] levelEntries, int level, IFieldSet fieldSet);
        protected abstract void DeleteEntry(T[] levelEntries, int level);
        protected abstract void DeleteFrom(T[] levelEntries, int n);
        protected abstract void DeleteThru(T[] levelEntries);
        protected abstract void InsertEntry(T[] levelEntries, int level, IFieldSet fieldSet);

        protected void HandleIncrementRefresh(T[] levelEntries, int level, MDUpdateAction updateAction, IFieldSet incrementEntry)
        {
            switch (updateAction)
            {
                case MDUpdateAction.Overlay:
                case MDUpdateAction.Change:
                    ModifyEntry(levelEntries, level, incrementEntry);
                    break;
                case MDUpdateAction.New:
                    InsertEntry(levelEntries, level, incrementEntry);
                    break;
                case MDUpdateAction.Delete:
                    DeleteEntry(levelEntries, level);
                    break;
                case MDUpdateAction.DeleteFrom:
                    DeleteFrom(levelEntries, level);
                    break;
                case MDUpdateAction.DeleteThru:
                    DeleteThru(levelEntries);
                    break;
                default:
                    throw new System.InvalidOperationException($"Unexpected MDUpdateAction: {updateAction}");
            }
        }
    }
}
