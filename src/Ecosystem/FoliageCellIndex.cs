using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Per-chunk index of deciduous foliage cells for random-tick season updates.</summary>
    internal sealed class FoliageCellIndex
    {
        readonly Dictionary<long, List<long>> cellsByChunk = new Dictionary<long, List<long>>();
        readonly Dictionary<long, long> chunkByPosKey = new Dictionary<long, long>();

        public int TotalCells
        {
            get
            {
                int n = 0;
                foreach (KeyValuePair<long, List<long>> kv in cellsByChunk)
                {
                    n += kv.Value.Count;
                }

                return n;
            }
        }

        public void Clear()
        {
            cellsByChunk.Clear();
            chunkByPosKey.Clear();
        }

        public void RemoveChunk(Vec2i chunkCoord)
        {
            long chunkKey = ChunkKey(chunkCoord.X, chunkCoord.Y);
            if (!cellsByChunk.TryGetValue(chunkKey, out List<long> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                chunkByPosKey.Remove(list[i]);
            }

            cellsByChunk.Remove(chunkKey);
        }

        public bool Contains(BlockPos pos) => chunkByPosKey.ContainsKey(PosKey(pos));

        public void Add(BlockPos pos)
        {
            if (pos == null) return;

            long posKey = PosKey(pos);
            if (chunkByPosKey.ContainsKey(posKey)) return;

            int cs = Vintagestory.API.Config.GlobalConstants.ChunkSize;
            long chunkKey = ChunkKey(pos.X / cs, pos.Z / cs);
            if (!cellsByChunk.TryGetValue(chunkKey, out List<long> list))
            {
                list = new List<long>();
                cellsByChunk[chunkKey] = list;
            }

            list.Add(posKey);
            chunkByPosKey[posKey] = chunkKey;
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null) return;

            long posKey = PosKey(pos);
            if (!chunkByPosKey.TryGetValue(posKey, out long chunkKey)) return;

            chunkByPosKey.Remove(posKey);
            if (!cellsByChunk.TryGetValue(chunkKey, out List<long> list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == posKey)
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            if (list.Count == 0)
            {
                cellsByChunk.Remove(chunkKey);
            }
        }

        public bool TryPickRandom(
            System.Random rnd,
            HashSet<long> activeChunkKeys,
            out BlockPos pos)
        {
            pos = null;
            if (rnd == null || cellsByChunk.Count == 0) return false;

            bool filterActive = activeChunkKeys != null && activeChunkKeys.Count > 0;
            int maxAttempts = filterActive
                ? System.Math.Min(128, activeChunkKeys.Count * 8)
                : System.Math.Min(128, cellsByChunk.Count * 4);

            while (maxAttempts-- > 0)
            {
                long chunkKey = filterActive
                    ? PickRandomActiveChunkWithCells(rnd, activeChunkKeys)
                    : PickRandomIndexedChunkKey(rnd);
                if (chunkKey == long.MinValue) continue;
                if (!cellsByChunk.TryGetValue(chunkKey, out List<long> list) || list.Count == 0) continue;

                long posKey = list[rnd.Next(list.Count)];
                pos = PosFromKey(posKey);
                return true;
            }

            return false;
        }

        long PickRandomActiveChunkWithCells(System.Random rnd, HashSet<long> activeChunkKeys)
        {
            int pick = rnd.Next(activeChunkKeys.Count);
            foreach (long key in activeChunkKeys)
            {
                if (pick-- != 0) continue;
                if (cellsByChunk.TryGetValue(key, out List<long> list) && list.Count > 0)
                {
                    return key;
                }

                break;
            }

            foreach (long key in activeChunkKeys)
            {
                if (cellsByChunk.TryGetValue(key, out List<long> list) && list.Count > 0)
                {
                    return key;
                }
            }

            return long.MinValue;
        }

        long PickRandomIndexedChunkKey(System.Random rnd)
        {
            int pick = rnd.Next(cellsByChunk.Count);
            foreach (KeyValuePair<long, List<long>> kv in cellsByChunk)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                if (pick-- == 0) return kv.Key;
            }

            using var e = cellsByChunk.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Value != null && e.Current.Value.Count > 0)
                {
                    return e.Current.Key;
                }
            }

            return long.MinValue;
        }

        public static long PosKey(BlockPos pos)
        {
            unchecked
            {
                return ((long)pos.X << 42) | ((long)(pos.Y & 0x1FFF) << 29) | (uint)pos.Z;
            }
        }

        public static BlockPos PosFromKey(long key)
        {
            int x = (int)(key >> 42);
            int y = (int)((key >> 29) & 0x1FFF);
            if (y > 4095) y -= 8192;
            int z = (int)(key & 0xFFFFFFFF);
            return new BlockPos(x, y, z);
        }

        public static long ChunkKey(int cx, int cz) => ((long)cx << 32) | (uint)cz;
    }
}
