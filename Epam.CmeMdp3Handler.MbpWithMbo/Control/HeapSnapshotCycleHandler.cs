/*
 * Copyright 2004-2016 EPAM Systems
 * This file is part of Java Market Data Handler for CME Market Data (MDP 3.0).
 * Java Market Data Handler for CME Market Data (MDP 3.0) is free software: you can redistribute it and/or modify it under the terms of the
 * GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Java Market Data Handler for CME Market Data (MDP 3.0) is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Java Market Data Handler for CME Market Data (MDP 3.0).
 * If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Heap-based (managed memory) implementation of <see cref="ISnapshotCycleHandler"/>.
    ///
    /// NOTE: This is a "dirty" implementation intended for unit tests and debugging.
    ///       It allocates data on the GC heap. <see cref="OffHeapSnapshotCycleHandler"/>
    ///       is preferred for production use.
    ///
    /// Java: com.epam.cme.mdp3.control.HeapSnapshotCycleHandler (@Deprecated)
    /// C# note: Marked [System.Obsolete] to match Java's @Deprecated annotation.
    ///          Java NativeBytesStore replaced by managed Dictionary + long[].
    /// </summary>
    [System.Obsolete("Dirty implementation for unit tests and debug. Use OffHeapSnapshotCycleHandler instead.")]
    public class HeapSnapshotCycleHandler : ISnapshotCycleHandler
    {
        private const long SnapshotSequenceUndefined = ISnapshotCycleHandler.SnapshotSequenceUndefined;

        private volatile Dictionary<int, long[]>? _metaData;
        private volatile int _metaDataSize;

        public void Reset()
        {
            _metaData = null;
        }

        public void Update(long totNumReports, long lastMsgSeqNumProcessed, int securityId, long noChunks, long currentChunk)
        {
            if (_metaData == null || _metaDataSize != totNumReports)
            {
                _metaDataSize = (int)totNumReports;
                _metaData = new Dictionary<int, long[]>(_metaDataSize);
            }

            if (!_metaData.TryGetValue(securityId, out long[]? securityIdMetaData) || securityIdMetaData == null)
            {
                securityIdMetaData = GetEmptyArray((int)noChunks);
                _metaData[securityId] = securityIdMetaData;
            }

            if (securityIdMetaData.Length != noChunks)
            {
                securityIdMetaData = GetEmptyArray((int)noChunks);
                _metaData[securityId] = securityIdMetaData;
            }

            securityIdMetaData[(int)currentChunk - 1] = lastMsgSeqNumProcessed;
        }

        public long GetSnapshotSequence(int securityId)
        {
            if (_metaData == null) return SnapshotSequenceUndefined;
            return _metaData.TryGetValue(securityId, out long[]? array) && array != null
                ? array[0]
                : SnapshotSequenceUndefined;
        }

        public long GetSmallestSnapshotSequence() => GetSnapshotSequence(false);

        public long GetHighestSnapshotSequence() => GetSnapshotSequence(true);

        private long GetSnapshotSequence(bool highest)
        {
            long sequence = SnapshotSequenceUndefined;
            bool result = true;

            if (_metaData != null && _metaData.Count == _metaDataSize)
            {
                foreach (long[] securityMetaData in _metaData.Values)
                {
                    for (int j = 0; j < securityMetaData.Length; j++)
                    {
                        long seq = securityMetaData[j];
                        if (seq != SnapshotSequenceUndefined)
                        {
                            if (sequence == SnapshotSequenceUndefined)
                            {
                                sequence = seq;
                            }
                            else if (highest && seq > sequence)
                            {
                                sequence = seq;
                            }
                            else if (!highest && seq < sequence)
                            {
                                sequence = seq;
                            }
                        }
                        else
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                result = false;
            }

            if (!result)
            {
                sequence = SnapshotSequenceUndefined;
            }

            return sequence;
        }

        private static long[] GetEmptyArray(int length)
        {
            long[] result = new long[length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = SnapshotSequenceUndefined;
            }
            return result;
        }
    }
}
