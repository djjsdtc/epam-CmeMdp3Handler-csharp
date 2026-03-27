using Epam.CmeMdp3Handler.Sbe.Message;

namespace Epam.CmeMdp3Handler.Control
{
    public class IncrementalRefreshQueue
    {
        private readonly IncrementalRefreshHolder[] _slots;
        private readonly int _queueSize;
        private long _lastRptSeqNum = 0;
        private readonly ISbeBuffer _sbeBuffer = new SbeBufferImpl();

        public IncrementalRefreshQueue(int size, int incrQueueEntryDefSize)
        {
            var buf = new byte[SbeConstants.MDP_PACKET_MAX_SIZE];
            _sbeBuffer.WrapForParse(buf, 0);
            _slots = new IncrementalRefreshHolder[size];
            _queueSize = size;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new IncrementalRefreshHolder(incrQueueEntryDefSize);
        }

        public bool Exist(long rptSeqNum)
        {
            int pos = (int)(rptSeqNum % _queueSize);
            return _slots[pos].Contains(rptSeqNum);
        }

        public int Poll(long rptSeqNum, IncrementalRefreshQueueEntry incrEntry)
        {
            int pos = (int)(rptSeqNum % _queueSize);
            return _slots[pos].Get(rptSeqNum, _sbeBuffer, incrEntry);
        }

        public bool Push(long rptSeqNum, IncrementalRefreshQueueEntry incrEntry)
        {
            int pos = (int)(rptSeqNum % _queueSize);
            bool res = _slots[pos].Put(incrEntry, rptSeqNum);
            if (res && rptSeqNum > _lastRptSeqNum)
                _lastRptSeqNum = rptSeqNum;
            return res;
        }

        public long GetLastRptSeqNum() => _lastRptSeqNum;

        public void Clear()
        {
            foreach (var slot in _slots) slot.Reset();
            _lastRptSeqNum = 0;
        }

        public void Release()
        {
            foreach (var slot in _slots) slot.Release();
            _lastRptSeqNum = 0;
        }

        public sealed class IncrementalRefreshQueueEntry
        {
            public IMdpGroupEntry GroupEntry;
            public short MatchEventIndicator;
            public long IncrPcktSeqNum;

            public IncrementalRefreshQueueEntry(IMdpGroupEntry groupEntry)
            {
                GroupEntry = groupEntry;
            }
        }
    }
}
