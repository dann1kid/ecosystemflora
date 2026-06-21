using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Round-robin live re-scan of player-near chunks for flora that appeared after load
    /// (mirrors <see cref="CyclicTreeTrunkScanner"/>).
    /// </summary>
    internal sealed class CyclicFloraScanner
    {
        readonly List<Vec2i> roundRobin = new List<Vec2i>();
        readonly Dictionary<long, Vec2i> columnResume = new Dictionary<long, Vec2i>();
        int roundRobinIndex;
        long activeChunkFingerprint;

        public void Clear()
        {
            roundRobin.Clear();
            columnResume.Clear();
            roundRobinIndex = 0;
            activeChunkFingerprint = 0;
        }

        public void ProcessTick(
            ICoreAPI api,
            IBlockAccessor acc,
            EcosystemConfig cfg,
            HashSet<long> activeChunkKeys,
            FloraColumnDiscovery.FloraFoundHandler onFound,
            ref int registrationsLeft,
            long budgetTicks,
            Stopwatch budgetWatch)
        {
            if (!cfg.EnableCyclicFloraDiscovery || api == null || acc == null || onFound == null) return;
            if (registrationsLeft <= 0 || cfg.MaxFloraRescanColumnsPerTick <= 0) return;
            if (activeChunkKeys == null || activeChunkKeys.Count == 0) return;

            RefreshChunkList(activeChunkKeys);
            if (roundRobin.Count == 0) return;

            int columnsLeft = cfg.MaxFloraRescanColumnsPerTick;
            int chunkPasses = 0;
            int maxChunkPasses = roundRobin.Count;

            while (columnsLeft > 0 && registrationsLeft > 0 && chunkPasses < maxChunkPasses)
            {
                if (budgetTicks > 0 && budgetWatch != null && budgetWatch.ElapsedTicks >= budgetTicks) break;

                if (roundRobinIndex >= roundRobin.Count) roundRobinIndex = 0;
                Vec2i chunk = roundRobin[roundRobinIndex];
                chunkPasses++;

                long chunkKey = ChunkKey(chunk.X, chunk.Y);
                if (!activeChunkKeys.Contains(chunkKey))
                {
                    columnResume.Remove(chunkKey);
                    roundRobinIndex++;
                    continue;
                }

                if (acc.GetMapChunk(chunk.X, chunk.Y) == null)
                {
                    roundRobinIndex++;
                    continue;
                }

                int startLx = 0;
                int startLz = 0;
                if (columnResume.TryGetValue(chunkKey, out Vec2i resume))
                {
                    startLx = resume.X;
                    startLz = resume.Y;
                }

                FloraColumnDiscovery.ScanResult scan = FloraColumnDiscovery.ScanChunkColumns(
                    api,
                    acc,
                    chunk,
                    onFound,
                    registrationsLeft,
                    columnsLeft,
                    startLx,
                    startLz);

                columnsLeft -= scan.ColumnsScanned;

                if (scan.Completed)
                {
                    columnResume.Remove(chunkKey);
                    roundRobinIndex++;
                }
                else
                {
                    columnResume[chunkKey] = new Vec2i(scan.ResumeLx, scan.ResumeLz);
                    break;
                }
            }
        }

        void RefreshChunkList(HashSet<long> activeChunkKeys)
        {
            long fingerprint = 0;
            foreach (long key in activeChunkKeys)
            {
                fingerprint ^= key;
            }

            if (fingerprint == activeChunkFingerprint && roundRobin.Count > 0) return;

            activeChunkFingerprint = fingerprint;
            roundRobin.Clear();
            roundRobinIndex = 0;
            columnResume.Clear();

            foreach (long key in activeChunkKeys)
            {
                int cx = (int)(key >> 32);
                int cz = (int)(key & 0xFFFFFFFF);
                roundRobin.Add(new Vec2i(cx, cz));
            }
        }

        static long ChunkKey(int cx, int cz) => ((long)cx << 32) | (uint)cz;
    }
}
