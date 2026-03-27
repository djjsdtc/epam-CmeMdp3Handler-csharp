using Epam.CmeMdp3Handler.Sbe.Message;
using Epam.CmeMdp3Handler.Sbe.Message.Meta;
using static Epam.CmeMdp3Handler.Control.IncrementalRefreshQueue;

namespace Epam.CmeMdp3Handler.Control
{
    public class IncrementalRefreshHolder
    {
        private long _rptSeqNumHolder;
        private byte[] _store;
        private int _entrySize;
        private SbeGroupType? _sbeGroupType;
        private short _matchEventIndicator;
        private long _incrPcktSeqNum;

        public IncrementalRefreshHolder(int incrQueueEntryDefSize)
        {
            _store = new byte[incrQueueEntryDefSize];
        }

        public bool Put(IncrementalRefreshQueueEntry queueEntry, long rptSeqNum)
        {
            if (_rptSeqNumHolder < rptSeqNum)
            {
                _rptSeqNumHolder = rptSeqNum;
                var incrEntry = queueEntry.GroupEntry;
                _entrySize = incrEntry.GetBlockLength();
                _matchEventIndicator = queueEntry.MatchEventIndicator;
                _incrPcktSeqNum = queueEntry.IncrPcktSeqNum;

                if (_store.Length < _entrySize)
                    _store = new byte[_entrySize];

                incrEntry.Buffer().CopyTo(incrEntry.GetAbsoluteEntryOffset(), _store, _entrySize);
                _sbeGroupType = incrEntry.GetSbeGroupType();
                return true;
            }
            return false;
        }

        public bool Contains(long rptSeqNum) => _rptSeqNumHolder == rptSeqNum;

        public int Get(long rptSeqNum, ISbeBuffer sbeBuffer, IncrementalRefreshQueueEntry queueEntry)
        {
            if (_rptSeqNumHolder == rptSeqNum)
            {
                var incrEntry = queueEntry.GroupEntry;
                var mutableEntry = (IMutableMdpGroupEntry)incrEntry;
                sbeBuffer.WrapForParse(_store, _entrySize);
                queueEntry.IncrPcktSeqNum = _incrPcktSeqNum;
                queueEntry.MatchEventIndicator = _matchEventIndicator;
                mutableEntry.Reset(sbeBuffer, _sbeGroupType!, 0, _entrySize);
                return _entrySize;
            }
            return 0;
        }

        public void Reset() => _rptSeqNumHolder = 0;

        public void Release() => _rptSeqNumHolder = 0;
    }
}
