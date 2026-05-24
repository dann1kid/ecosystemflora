using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Caches static worldgen climate per XZ column (rain, forest).</summary>
    internal sealed class EnvironmentalColumnCache
    {
        struct WorldgenColumn
        {
            public float WorldgenRainfall;
            public float ForestDensity;
            public bool HasClimate;
        }

        readonly Dictionary<long, WorldgenColumn> worldgenByXz = new Dictionary<long, WorldgenColumn>();
        const int MaxWorldgenEntries = 65536;
        readonly Queue<long> worldgenOrder = new Queue<long>();

        public bool TryGetWorldgen(IBlockAccessor acc, BlockPos plantPos, out float rainfall, out float forestDensity, out bool hasClimate)
        {
            rainfall = 0f;
            forestDensity = 0f;
            hasClimate = false;
            if (acc == null || plantPos == null) return false;

            long key = ColumnKey(plantPos.X, plantPos.Z);
            if (worldgenByXz.TryGetValue(key, out WorldgenColumn cached))
            {
                rainfall = cached.WorldgenRainfall;
                forestDensity = cached.ForestDensity;
                hasClimate = cached.HasClimate;
                return true;
            }

            ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            ClimateCondition fallback = worldgen ?? now;

            cached = new WorldgenColumn
            {
                WorldgenRainfall = worldgen?.WorldgenRainfall ?? now?.WorldgenRainfall ?? 0f,
                ForestDensity = worldgen?.ForestDensity ?? now?.ForestDensity ?? 0f,
                HasClimate = fallback != null,
            };

            StoreWorldgen(key, cached);
            rainfall = cached.WorldgenRainfall;
            forestDensity = cached.ForestDensity;
            hasClimate = cached.HasClimate;
            return true;
        }

        public void InvalidateColumn(int x, int z)
        {
            worldgenByXz.Remove(ColumnKey(x, z));
        }

        public void InvalidateAround(BlockPos pos, int horizontalRadius)
        {
            if (pos == null) return;

            for (int dx = -horizontalRadius; dx <= horizontalRadius; dx++)
            {
                for (int dz = -horizontalRadius; dz <= horizontalRadius; dz++)
                {
                    InvalidateColumn(pos.X + dx, pos.Z + dz);
                }
            }
        }

        public void Clear()
        {
            worldgenByXz.Clear();
            worldgenOrder.Clear();
        }

        void StoreWorldgen(long key, WorldgenColumn value)
        {
            if (!worldgenByXz.ContainsKey(key))
            {
                worldgenOrder.Enqueue(key);
                while (worldgenOrder.Count > MaxWorldgenEntries)
                {
                    long old = worldgenOrder.Dequeue();
                    worldgenByXz.Remove(old);
                }
            }

            worldgenByXz[key] = value;
        }

        static long ColumnKey(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
