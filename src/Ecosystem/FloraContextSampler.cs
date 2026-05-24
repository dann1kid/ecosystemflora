using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Local open / forest-edge / forest-interior from neighboring tree blocks.</summary>
    internal sealed class FloraContextSampler
    {
        readonly Dictionary<long, CachedColumn> cache = new Dictionary<long, CachedColumn>();
        readonly Queue<long> cacheOrder = new Queue<long>();
        const int MaxCacheEntries = 8192;

        struct CachedColumn
        {
            public FloraContext Context;
            public int ForestNeighbors;
            public double CachedAtHours;
        }

        public FloraContext GetContext(ICoreAPI api, BlockPos pos)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseFloraContext || api == null || pos == null)
            {
                return FloraContext.Open;
            }

            long key = ColumnKey(pos.X, pos.Z);
            double now = api.World.Calendar.TotalHours;
            if (cache.TryGetValue(key, out CachedColumn cached)
                && now - cached.CachedAtHours < cfg.FloraContextCacheHours)
            {
                return cached.Context;
            }

            int forestNeighbors = CountForestNeighbors(api.World.BlockAccessor, pos, cfg.FloraContextNeighborRadius);
            FloraContext context = Classify(forestNeighbors, cfg.FloraContextInteriorThreshold);

            StoreCache(key, new CachedColumn
            {
                Context = context,
                ForestNeighbors = forestNeighbors,
                CachedAtHours = now,
            });

            return context;
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
            int count = 0;
            int y0 = center.Y - 1;
            int y1 = center.Y + 2;
            var scanPos = new BlockPos();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    for (int y = y0; y <= y1; y++)
                    {
                        scanPos.Set(center.X + dx, y, center.Z + dz);
                        Block block = acc.GetBlock(scanPos);
                        if (IsForestNeighborBlock(block))
                        {
                            count++;
                            if (count >= 8) return count;
                        }
                    }
                }
            }

            return count;
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
