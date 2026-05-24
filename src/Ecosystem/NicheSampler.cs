using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Local moisture + light from ground block, fluid neighbors, and sunlight.</summary>
    internal sealed class NicheSampler
    {
        readonly Dictionary<long, CachedCell> cache = new Dictionary<long, CachedCell>();
        readonly Queue<long> cacheOrder = new Queue<long>();
        const int MaxCacheEntries = 8192;

        struct CachedCell
        {
            public LocalNiche Niche;
            public double CachedAtHours;
        }

        public LocalNiche GetNiche(ICoreAPI api, BlockPos pos)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseNicheContext || api == null || pos == null)
            {
                return DefaultNiche;
            }

            long key = PosKey(pos.X, pos.Y, pos.Z);
            double now = api.World.Calendar.TotalHours;
            if (cache.TryGetValue(key, out CachedCell cached)
                && now - cached.CachedAtHours < cfg.NicheCacheHours)
            {
                return cached.Niche;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            LocalNiche niche = new LocalNiche(
                ClassifyMoisture(acc, pos),
                ClassifyLight(acc, pos));

            StoreCache(key, new CachedCell
            {
                Niche = niche,
                CachedAtHours = now,
            });

            return niche;
        }

        public void InvalidateAround(BlockPos pos, int radius)
        {
            if (pos == null) return;

            int vertical = 3;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -vertical; dy <= vertical; dy++)
                    {
                        cache.Remove(PosKey(pos.X + dx, pos.Y + dy, pos.Z + dz));
                    }
                }
            }
        }

        public void Clear()
        {
            cache.Clear();
            cacheOrder.Clear();
        }

        static readonly LocalNiche DefaultNiche = new LocalNiche(MoistureLevel.Mesic, LightLevel.Partial);

        internal static MoistureLevel ClassifyMoisture(IBlockAccessor acc, BlockPos plantPos)
        {
            Block ground = acc.GetBlock(plantPos.DownCopy());
            string path = ground?.Code?.Path ?? "";

            if (path == "peat")
            {
                return MoistureLevel.Wet;
            }

            if (BlockFluidHelper.IsMuddyGravel(ground))
            {
                if (BlockFluidHelper.TouchesFluid(acc, plantPos))
                {
                    return MoistureLevel.Wet;
                }

                return MoistureLevel.Shoreline;
            }

            if (HasNearbyWater(acc, plantPos, 2))
            {
                return MoistureLevel.Wet;
            }

            if (BlockFluidHelper.TouchesFluid(acc, plantPos))
            {
                return MoistureLevel.Shoreline;
            }

            SoilKind kinds = SoilClassification.Classify(ground);
            if ((kinds & (SoilKind.Sand | SoilKind.Barren)) != 0)
            {
                return MoistureLevel.Dry;
            }

            return MoistureLevel.Mesic;
        }

        internal static LightLevel ClassifyLight(IBlockAccessor acc, BlockPos plantPos)
        {
            int light = acc.GetLightLevel(plantPos, EnumLightLevelType.OnlySunLight);
            if (light >= 11) return LightLevel.Open;
            if (light >= 9) return LightLevel.Partial;
            if (light >= 7) return LightLevel.Shade;
            return LightLevel.DeepShade;
        }

        static bool HasNearbyWater(IBlockAccessor acc, BlockPos center, int radius)
        {
            var scanPos = new BlockPos();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        scanPos.Set(center.X + dx, center.Y + dy, center.Z + dz);
                        if (BlockFluidHelper.IsWaterAt(acc, scanPos))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        void StoreCache(long key, CachedCell value)
        {
            if (!cache.ContainsKey(key))
            {
                cacheOrder.Enqueue(key);
                while (cacheOrder.Count > MaxCacheEntries)
                {
                    long old = cacheOrder.Dequeue();
                    cache.Remove(old);
                }
            }

            cache[key] = value;
        }

        static long PosKey(int x, int y, int z)
        {
            unchecked
            {
                return ((long)x << 42) | ((long)(y & 0x1FFF) << 29) | (uint)z;
            }
        }
    }
}
