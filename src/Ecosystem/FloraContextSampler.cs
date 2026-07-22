using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Local open / forest-edge / forest-interior and forest cover from neighboring tree blocks.</summary>
    internal sealed class FloraContextSampler
    {
        public const int ForestCoverNeighborCap = 8;

        readonly Dictionary<long, CachedColumn> cache = new Dictionary<long, CachedColumn>();
        readonly Queue<long> cacheOrder = new Queue<long>();
        const int MaxCacheEntries = 8192;

        struct CachedColumn
        {
            public FloraContext Context;
            public float LocalForestCover;
            public int ForestNeighbors;
            public double CachedAtHours;
        }

        public FloraContext GetContext(ICoreAPI api, BlockPos pos)
        {
            return Sample(api, pos).Context;
        }

        public float GetLocalForestCover(ICoreAPI api, BlockPos pos)
        {
            return Sample(api, pos).LocalForestCover;
        }

        CachedColumn Sample(ICoreAPI api, BlockPos pos)
        {
            if (api == null || pos == null)
            {
                return new CachedColumn
                {
                    Context = FloraContext.Open,
                    LocalForestCover = 0f,
                    CachedAtHours = 0,
                };
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            long key = ColumnKey(pos.X, pos.Z);
            double now = api.World.Calendar.TotalHours;
            if (cache.TryGetValue(key, out CachedColumn cached)
                && now - cached.CachedAtHours < cfg.FloraContextCacheHours)
            {
                return cached;
            }

            int forestNeighbors = CountForestNeighbors(api.World.BlockAccessor, pos, cfg.FloraContextNeighborRadius);
            FloraContext context = cfg.UseFloraContext
                ? Classify(forestNeighbors, cfg.FloraContextInteriorThreshold)
                : FloraContext.Open;

            cached = new CachedColumn
            {
                Context = context,
                ForestNeighbors = forestNeighbors,
                LocalForestCover = CoverFromNeighborCount(forestNeighbors),
                CachedAtHours = now,
            };

            StoreCache(key, cached);
            return cached;
        }

        public static float CoverFromNeighborCount(int forestNeighbors)
        {
            if (forestNeighbors <= 0) return 0f;
            return Math.Min(1f, forestNeighbors / (float)ForestCoverNeighborCap);
        }

        public void InvalidateAround(BlockPos pos, int radius)
        {
            if (pos == null) return;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int r = radius + cfg.FloraContextNeighborRadius;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    cache.Remove(ColumnKey(pos.X + dx, pos.Z + dz));
                }
            }
        }

        public void Clear()
        {
            cache.Clear();
            cacheOrder.Clear();
        }

        static FloraContext Classify(int forestNeighbors, int interiorThreshold)
        {
            if (forestNeighbors >= interiorThreshold) return FloraContext.ForestInterior;
            if (forestNeighbors >= 1) return FloraContext.ForestEdge;
            return FloraContext.Open;
        }

        static int CountForestNeighbors(IBlockAccessor acc, BlockPos center, int radius)
        {
            return CountForestNeighbors(acc, center, radius, excludeRadius: 0);
        }

        /// <summary>
        /// Forest neighbor count for niche checks on a standing tree: columns within
        /// <paramref name="excludeRadius"/> (Chebyshev) of the trunk are ignored so the tree's own
        /// crown does not inflate local cover.
        /// </summary>
        public static int CountForestNeighbors(
            IBlockAccessor acc,
            BlockPos center,
            int radius,
            int excludeRadius)
        {
            int count = 0;
            if (acc == null || center == null) return 0;
            if (radius < 0) radius = 0;
            if (excludeRadius < 0) excludeRadius = 0;

            int y0 = center.Y - 1;
            int y1 = center.Y + 2;
            var scanPos = new BlockPos(0);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    if (excludeRadius > 0)
                    {
                        int chebyshev = dx < 0 ? -dx : dx;
                        int az = dz < 0 ? -dz : dz;
                        if (az > chebyshev) chebyshev = az;
                        if (chebyshev <= excludeRadius) continue;
                    }

                    for (int y = y0; y <= y1; y++)
                    {
                        scanPos.Set(center.X + dx, y, center.Z + dz);
                        Block block = acc.GetBlock(scanPos);
                        if (IsForestNeighborBlock(block))
                        {
                            count++;
                            if (count >= ForestCoverNeighborCap) return count;
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Local forest cover excluding the trunk's own crown footprint (for tree niche lifespan).
        /// Scan radius is capped so giant crowns do not fan out a huge GetBlock grid each year.
        /// </summary>
        public float GetLocalForestCoverExcludingSelf(ICoreAPI api, BlockPos trunkBase, int crownRadius)
        {
            if (api == null || trunkBase == null) return 0f;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            const int MaxNicheForestScanRadius = 6;
            int exclude = crownRadius < 1 ? 1 : crownRadius;
            if (exclude > MaxNicheForestScanRadius - 1)
            {
                exclude = MaxNicheForestScanRadius - 1;
            }

            int radius = cfg.FloraContextNeighborRadius;
            if (radius <= exclude) radius = exclude + 1;
            if (radius > MaxNicheForestScanRadius) radius = MaxNicheForestScanRadius;

            int forestNeighbors = CountForestNeighbors(
                api.World.BlockAccessor,
                trunkBase,
                radius,
                exclude);
            return CoverFromNeighborCount(forestNeighbors);
        }

        internal static bool IsForestNeighborBlock(Block block)
        {
            if (block?.Code == null || block.Id == 0) return false;
            if (block.Code.Domain != "game") return false;

            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path)) return false;

            if (path.StartsWith("log-grown-"))
            {
                return !path.StartsWith("log-grown-aged");
            }

            if (path.StartsWith("ferntree-normal-trunk"))
            {
                return true;
            }

            if (path.StartsWith("leaves-") || path.StartsWith("plaintreesapling-"))
            {
                return true;
            }

            return false;
        }

        void StoreCache(long key, CachedColumn value)
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

        static long ColumnKey(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
