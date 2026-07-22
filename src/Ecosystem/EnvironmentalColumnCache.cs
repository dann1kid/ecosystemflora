using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Caches worldgen rainfall (permanent) and NowValues temperature (per tick generation).</summary>
    internal sealed class EnvironmentalColumnCache
    {
        struct WorldgenColumn
        {
            public float WorldgenRainfall;
            public float WorldgenTemperature;
            public bool HasClimate;
        }

        struct NowColumn
        {
            public float Temperature;
            public long Generation;
        }

        readonly Dictionary<long, WorldgenColumn> worldgenByXz = new Dictionary<long, WorldgenColumn>();
        const int MaxWorldgenEntries = 65536;
        readonly Queue<long> worldgenOrder = new Queue<long>();

        readonly Dictionary<long, NowColumn> nowByXz = new Dictionary<long, NowColumn>();
        const int MaxNowEntries = 4096;
        readonly Queue<long> nowOrder = new Queue<long>();
        long currentGeneration;

        /// <summary>Call once at the start of each reproduce tick to invalidate stale NowValues.</summary>
        public void AdvanceGeneration() => currentGeneration++;

        public bool TryGetWorldgenRainfall(IBlockAccessor acc, BlockPos plantPos, out float rainfall, out bool hasClimate)
        {
            return TryGetWorldgenClimate(acc, plantPos, out rainfall, out _, out hasClimate);
        }

        /// <summary>Worldgen rainfall + temperature from one climate lookup (cached per column).</summary>
        public bool TryGetWorldgenClimate(
            IBlockAccessor acc,
            BlockPos plantPos,
            out float rainfall,
            out float temperature,
            out bool hasClimate)
        {
            rainfall = 0f;
            temperature = 0f;
            hasClimate = false;
            if (acc == null || plantPos == null) return false;

            long key = ColumnKey(plantPos.X, plantPos.Z);
            if (worldgenByXz.TryGetValue(key, out WorldgenColumn cached))
            {
                rainfall = cached.WorldgenRainfall;
                temperature = cached.WorldgenTemperature;
                hasClimate = cached.HasClimate;
                return true;
            }

            ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            ClimateCondition fallback = worldgen ?? now;

            cached = new WorldgenColumn
            {
                WorldgenRainfall = worldgen?.WorldgenRainfall ?? now?.WorldgenRainfall ?? 0f,
                WorldgenTemperature = worldgen?.Temperature ?? now?.Temperature ?? 0f,
                HasClimate = fallback != null,
            };

            StoreWorldgen(key, cached);
            rainfall = cached.WorldgenRainfall;
            temperature = cached.WorldgenTemperature;
            hasClimate = cached.HasClimate;
            return true;
        }

        public bool TryGetNowTemperature(IBlockAccessor acc, BlockPos plantPos, out float temperature)
        {
            temperature = 0f;
            if (acc == null || plantPos == null) return false;

            long key = ColumnKey(plantPos.X, plantPos.Z);
            if (nowByXz.TryGetValue(key, out NowColumn cached) && cached.Generation == currentGeneration)
            {
                temperature = cached.Temperature;
                return true;
            }

            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            temperature = now?.Temperature ?? 0f;

            StoreNow(key, new NowColumn { Temperature = temperature, Generation = currentGeneration });
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
            nowByXz.Clear();
            nowOrder.Clear();
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

        void StoreNow(long key, NowColumn value)
        {
            if (!nowByXz.ContainsKey(key))
            {
                nowOrder.Enqueue(key);
                while (nowOrder.Count > MaxNowEntries)
                {
                    long old = nowOrder.Dequeue();
                    nowByXz.Remove(old);
                }
            }

            nowByXz[key] = value;
        }

        static long ColumnKey(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
