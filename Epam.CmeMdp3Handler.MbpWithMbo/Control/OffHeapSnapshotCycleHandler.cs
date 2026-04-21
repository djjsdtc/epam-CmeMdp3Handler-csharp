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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Managed-heap implementation of <see cref="ISnapshotCycleHandler"/>.
    ///
    /// Java: com.epam.cme.mdp3.control.OffHeapSnapshotCycleHandler
    /// C# note: Java uses Chronicle Bytes (NativeBytesStore / BytesStore) for off-heap
    ///          storage of per-security chunk arrays. C# replaces this with plain long[]
    ///          arrays managed on the GC heap (analogous to HeapSnapshotCycleHandler),
    ///          because .NET does not have a direct equivalent to Chronicle Bytes.
    ///          The agrona Long2ObjectHashMap is replaced by Dictionary&lt;long, T&gt;.
    ///          The Apache Commons EqualsBuilder/HashCodeBuilder are replaced by
    ///          standard C# ValueTuple equality.
    /// </summary>
    public class OffHeapSnapshotCycleHandler : ISnapshotCycleHandler
    {
        private static readonly ILogger Logger =
            LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OffHeapSnapshotCycleHandler>();

        private const long SnapshotSequenceUndefined = ISnapshotCycleHandler.SnapshotSequenceUndefined;

        // Cache of previously allocated entries, indexed by securityId, reused across cycles
        private readonly Dictionary<long, MutableLongToLongArrayPair> _dataCache = new();

        // Active data for the current snapshot cycle, indexed by securityId
        private readonly Dictionary<long, MutableLongToLongArrayPair> _data = new();

        private volatile int _dataSize;

        public void Reset()
        {
            foreach (var pair in _data.Values)
            {
                ClearArray(pair.Value);
                pair.Key = 0;
            }
        }

        public void Update(long totNumReports, long lastMsgSeqNumProcessed, int securityId, long noChunks, long currentChunk)
        {
            if (currentChunk > noChunks)
            {
                Logger.LogError("Current chunk number '{CurrentChunk}' is more than noChunks number '{NoChunks}' for securityId '{SecurityId}'",
                    currentChunk, noChunks, securityId);
                return;
            }

            if (_dataSize != totNumReports)
            {
                _dataSize = (int)totNumReports;
                // Move current data to cache, clear active data
                foreach (var kv in _data)
                    _dataCache[kv.Key] = kv.Value;
                _data.Clear();
            }

            if (!_data.TryGetValue(securityId, out MutableLongToLongArrayPair? securityIdMetaData))
            {
                if (_dataCache.TryGetValue(securityId, out securityIdMetaData))
                {
                    _dataCache.Remove(securityId);
                }
                else
                {
                    long arrayLength = noChunks > ISnapshotCycleHandler.MaxNoChunkValue
                        ? noChunks
                        : ISnapshotCycleHandler.MaxNoChunkValue;
                    long[] newArray = new long[arrayLength];
                    ClearArray(newArray);
                    securityIdMetaData = new MutableLongToLongArrayPair(noChunks, newArray);
                }
                _data[securityId] = securityIdMetaData;
            }

            long[] currentArray = securityIdMetaData.Value;
            if (securityIdMetaData.Key != noChunks)
            {
                if (currentArray.Length < noChunks)
                {
                    currentArray = new long[noChunks];
                    securityIdMetaData.Value = currentArray;
                }
                securityIdMetaData.Key = noChunks;
                ClearArray(currentArray);
            }

            currentArray[currentChunk - 1] = lastMsgSeqNumProcessed;
        }

        public long GetSnapshotSequence(int securityId)
        {
            return _data.TryGetValue(securityId, out MutableLongToLongArrayPair? pair)
                ? pair.Value[0]
                : SnapshotSequenceUndefined;
        }

        public long GetSmallestSnapshotSequence() => GetSnapshotSequence(false);

        public long GetHighestSnapshotSequence() => GetSnapshotSequence(true);

        private long GetSnapshotSequence(bool highest)
        {
            long result = SnapshotSequenceUndefined;
            bool existUndefined = false;

            if (_data.Count == _dataSize)
            {
                foreach (MutableLongToLongArrayPair pair in _data.Values)
                {
                    for (int j = 0; j < pair.Key; j++)
                    {
                        long seq = pair.Value[j];
                        if (seq != SnapshotSequenceUndefined)
                        {
                            if (result == SnapshotSequenceUndefined)
                            {
                                result = seq;
                            }
                            else if (highest && seq > result)
                            {
                                result = seq;
                            }
                            else if (!highest && seq < result)
                            {
                                result = seq;
                            }
                        }
                        else
                        {
                            existUndefined = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                existUndefined = true;
            }

            if (existUndefined)
            {
                result = SnapshotSequenceUndefined;
            }

            return result;
        }

        private static void ClearArray(long[] array)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = SnapshotSequenceUndefined;
        }

        /// <summary>
        /// Mutable pair of (noChunks key, long[] array value).
        ///
        /// Java: OffHeapSnapshotCycleHandler.MutableLongToObjPair&lt;LongArray&gt;
        /// C# note: Java used Apache Commons EqualsBuilder/HashCodeBuilder; C# uses
        ///          default object equality (reference equality) which is sufficient here
        ///          since pairs are stored in dictionaries keyed by securityId.
        /// </summary>
        private sealed class MutableLongToLongArrayPair
        {
            public long Key;
            public long[] Value;

            public MutableLongToLongArrayPair(long key, long[] value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}
