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

namespace Epam.CmeMdp3Handler.MbpWithMbo.Control
{
    /// <summary>
    /// Tracks snapshot cycles per security to detect snapshot boundaries and gaps.
    ///
    /// Java: com.epam.cme.mdp3.control.SnapshotCycleHandler
    /// </summary>
    public interface ISnapshotCycleHandler
    {
        /// <summary>Undefined snapshot sequence sentinel value.</summary>
        const long SnapshotSequenceUndefined = -1;

        /// <summary>Maximum no-chunk value for off-heap array pre-allocation.</summary>
        const long MaxNoChunkValue = 400;

        void Reset();

        void Update(long totNumReports, long lastMsgSeqNumProcessed, int securityId, long noChunks, long currentChunk);

        long GetSnapshotSequence(int securityId);

        /// <returns>
        /// The smallest snapshot sequence, or <see cref="SnapshotSequenceUndefined"/> if there are gaps.
        /// </returns>
        long GetSmallestSnapshotSequence();

        /// <returns>
        /// The highest snapshot sequence, or <see cref="SnapshotSequenceUndefined"/> if there are gaps.
        /// </returns>
        long GetHighestSnapshotSequence();
    }
}
