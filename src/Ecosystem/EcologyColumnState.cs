using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Unified spread cell cache: block layers, worldgen rain, and local forest cover per plant cell.
    /// Invalidation hub for spread path (Phase 6.4).
    /// </summary>
    internal sealed class EcologyColumnState
    {
        readonly Dictionary<long, SpreadColumnSnapshot> spreadByCell = new Dictionary<long, SpreadColumnSnapshot>();
        readonly Queue<long> spreadOrder = new Queue<long>();
        const int MaxSpreadEntries = 16384;

        public int CacheSize => spreadByCell.Count;

        public long CacheHits { get; private set; }

        public long CacheMisses { get; private set; }

        public bool TryGetSpreadSnapshot(ICoreAPI api, BlockPos plantPos, out SpreadColumnSnapshot snapshot)
        {
            snapshot = default;
            if (api == null || plantPos == null) return false;

            long key = CellKey(plantPos.X, plantPos.Y, plantPos.Z);
            if (spreadByCell.TryGetValue(key, out snapshot))
            {
                CacheHits++;
                return true;
            }

            CacheMisses++;
            snapshot = BuildSnapshot(api, plantPos);
            StoreSpread(key, snapshot);
            return true;
        }

        SpreadColumnSnapshot BuildSnapshot(ICoreAPI api, BlockPos plantPos)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            CellBlockSnapshot blockSnap = CellBlockSnapshot.Sample(acc, plantPos);

            float worldgenRainfall = 0f;
            bool hasClimate = false;
            EnvironmentalColumnCache climateCache = EcosystemSystem.Instance?.ColumnCache;
            if (climateCache != null && climateCache.TryGetWorldgenRainfall(acc, plantPos, out worldgenRainfall, out hasClimate))
            {
                // cached worldgen column
            }
            else
            {
                ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
                ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
                ClimateCondition fallback = worldgen ?? now;
                worldgenRainfall = worldgen?.WorldgenRainfall ?? now?.WorldgenRainfall ?? 0f;
                hasClimate = fallback != null;
            }

            float localForestCover = 0f;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            if (flora != null)
            {
                localForestCover = flora.GetLocalForestCover(api, plantPos);
            }

            return new SpreadColumnSnapshot(blockSnap, worldgenRainfall, hasClimate, localForestCover);
        }

        public void InvalidateColumn(int x, int y, int z)
        {
            spreadByCell.Remove(CellKey(x, y, z));
        }

        public void InvalidateAround(BlockPos pos, int horizontalRadius)
        {
            if (pos == null) return;

            int vertical = 3;
            for (int dx = -horizontalRadius; dx <= horizontalRadius; dx++)
            {
                for (int dz = -horizontalRadius; dz <= horizontalRadius; dz++)
                {
                    for (int dy = -vertical; dy <= vertical; dy++)
                    {
                        spreadByCell.Remove(CellKey(pos.X + dx, pos.Y + dy, pos.Z + dz));
                    }
                }
            }
        }

        public void Clear()
        {
            spreadByCell.Clear();
            spreadOrder.Clear();
            CacheHits = 0;
            CacheMisses = 0;
        }

        void StoreSpread(long key, SpreadColumnSnapshot snapshot)
        {
            if (!spreadByCell.ContainsKey(key))
            {
                spreadOrder.Enqueue(key);
                while (spreadOrder.Count > MaxSpreadEntries)
                {
                    long old = spreadOrder.Dequeue();
                    spreadByCell.Remove(old);
                }
            }

            spreadByCell[key] = snapshot;
        }

        internal static long CellKey(int x, int y, int z)
        {
            unchecked
            {
                return ((long)x << 42) | ((long)(y & 0x1FFF) << 29) | (uint)z;
            }
        }
    }
}
