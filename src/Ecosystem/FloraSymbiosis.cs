using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Symbionts need a host to spread and survive. When a host vanishes, cache entries are
    /// dropped and nearby ecology is woken; orphaned symbionts fade via the stress tick
    /// (<see cref="EcosystemSystem"/> FailedSurvivalChecks), not instant removal.
    /// </summary>
    internal static class FloraSymbiosis
    {
        public const string TreeHostToken = "tree";

        /// <summary>Ground symbionts can sit many blocks below canopy logs.</summary>
        const int TreeHostVerticalSearchUp = 16;
        const int NonTreeVerticalSearch = 2;

        public readonly struct Rule
        {
            public readonly string[] HostKeys;
            public readonly int MaxHostDistance;

            public Rule(string[] hostKeys, int maxHostDistance)
            {
                HostKeys = hostKeys;
                MaxHostDistance = maxHostDistance;
            }
        }

        static readonly Dictionary<string, Rule> SymbiontRules = BuildRules();

        static readonly Dictionary<long, BlockPos> hostCache = new Dictionary<long, BlockPos>();
        static readonly Queue<long> hostCacheOrder = new Queue<long>();
        const int MaxHostCacheEntries = 4096;
        /// <summary>Sentinel: host lookup was done and nothing was found.</summary>
        static readonly BlockPos NoHost = new BlockPos(-1, -1, -1, 0);

        static Dictionary<string, Rule> BuildRules()
        {
            return new Dictionary<string, Rule>
            {
                ["lilyofthevalley"] = new Rule(new[] { TreeHostToken }, 3),
                ["bluebell"] = new Rule(new[] { TreeHostToken }, 3),
                ["eaglefern"] = new Rule(new[] { TreeHostToken }, 2),
                ["cinnamonfern"] = new Rule(new[] { TreeHostToken }, 2),
                ["deerfern"] = new Rule(new[] { TreeHostToken }, 2),
                ["tallfern"] = new Rule(new[] { TreeHostToken }, 2),
                ["blackcurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["redcurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["whitecurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["blackberry"] = new Rule(new[] { TreeHostToken }, 3),
                ["raspberry"] = new Rule(new[] { TreeHostToken }, 3),
            };
        }

        public static bool TryGetRule(string symbiontSpecies, out Rule rule)
        {
            rule = default;
            if (string.IsNullOrEmpty(symbiontSpecies)) return false;
            return SymbiontRules.TryGetValue(symbiontSpecies, out rule);
        }

        public static bool CanSpread(IBlockAccessor acc, BlockPos symbiontPos, string symbiontSpecies)
        {
            if (!EcosystemConfig.Loaded.EnableSymbiosis) return true;
            if (!TryGetRule(symbiontSpecies, out _)) return true;
            return HasRequiredHost(acc, symbiontPos, symbiontSpecies);
        }

        public static bool HasRequiredHost(IBlockAccessor acc, BlockPos symbiontPos, string symbiontSpecies)
        {
            if (!TryGetRule(symbiontSpecies, out Rule rule)) return true;

            long key = PosKey(symbiontPos);
            if (hostCache.TryGetValue(key, out BlockPos cached))
            {
                if (!ReferenceEquals(cached, NoHost))
                {
                    Block hostBlock = acc.GetBlock(cached);
                    if (hostBlock != null && hostBlock.Id != 0 && MatchesHostRule(rule, hostBlock))
                    {
                        return true;
                    }
                }
                hostCache.Remove(key);
            }

            BlockPos found = FindHost(acc, symbiontPos, rule, out _, out BlockPos hostPos);
            if (found != null)
            {
                StoreHostCache(key, hostPos);
            }
            return found != null;
        }

        /// <summary>
        /// Call when a host block is removed (tree felled, spread parent broken, etc.).
        /// Does not remove symbionts — stress death handles orphans on the normal recheck cadence.
        /// </summary>
        public static void NotifyHostRemoved(ICoreAPI api, BlockPos hostPos, Block hostBlock)
        {
            if (api == null || hostPos == null || !EcosystemConfig.Loaded.EnableSymbiosis) return;

            int radius = EcosystemConfig.Loaded.SymbiosisCascadeRadius;
            InvalidateHostCacheAround(hostPos, radius);
            EcosystemSystem.Instance?.WakeEcologyAround(hostPos);
        }

        static bool SymbiontLinkedToRemovedHost(Rule rule, Block hostBlock, BlockPos hostPos, BlockPos symbiontPos)
        {
            if (hostBlock == null) return false;

            if (HorizontalChebyshev(hostPos, symbiontPos) > rule.MaxHostDistance) return false;

            for (int i = 0; i < rule.HostKeys.Length; i++)
            {
                string key = rule.HostKeys[i];
                if (key == TreeHostToken && PlantCodeHelper.IsArborealHostBlock(hostBlock))
                {
                    return true;
                }

                string hostSpecies = PlantCodeHelper.ResolveEcologySpecies(hostBlock);
                if (hostSpecies != null && hostSpecies == key)
                {
                    return true;
                }
            }

            return false;
        }

        static BlockPos FindHost(IBlockAccessor acc, BlockPos symbiontPos, Rule rule, out Block hostBlock, out BlockPos hostPos)
        {
            hostBlock = null;
            hostPos = null;

            if (RuleUsesTreeHost(rule))
            {
                return FindTreeHostAbove(acc, symbiontPos, rule.MaxHostDistance, out hostBlock, out hostPos);
            }

            var scanPos = new BlockPos(0);
            for (int dx = -rule.MaxHostDistance; dx <= rule.MaxHostDistance; dx++)
            {
                for (int dz = -rule.MaxHostDistance; dz <= rule.MaxHostDistance; dz++)
                {
                    for (int dy = -NonTreeVerticalSearch; dy <= NonTreeVerticalSearch; dy++)
                    {
                        scanPos.Set(symbiontPos.X + dx, symbiontPos.Y + dy, symbiontPos.Z + dz);
                        if (scanPos.Equals(symbiontPos)) continue;

                        Block block = acc.GetBlock(scanPos);
                        if (block.Id == 0) continue;

                        if (SymbiontLinkedToRemovedHost(rule, block, scanPos, symbiontPos))
                        {
                            hostBlock = block;
                            hostPos = scanPos.Copy();
                            return hostPos;
                        }
                    }
                }
            }

            return null;
        }

        static BlockPos FindTreeHostAbove(
            IBlockAccessor acc,
            BlockPos symbiontPos,
            int maxHorizontalDistance,
            out Block hostBlock,
            out BlockPos hostPos)
        {
            hostBlock = null;
            hostPos = null;
            var scanPos = new BlockPos(0);

            for (int dx = -maxHorizontalDistance; dx <= maxHorizontalDistance; dx++)
            {
                for (int dz = -maxHorizontalDistance; dz <= maxHorizontalDistance; dz++)
                {
                    for (int dy = 1; dy <= TreeHostVerticalSearchUp; dy++)
                    {
                        scanPos.Set(symbiontPos.X + dx, symbiontPos.Y + dy, symbiontPos.Z + dz);
                        if (scanPos.Equals(symbiontPos)) continue;

                        if (HorizontalChebyshev(symbiontPos, scanPos) > maxHorizontalDistance) continue;

                        Block block = acc.GetBlock(scanPos);
                        if (!PlantCodeHelper.IsArborealHostBlock(block)) continue;

                        hostBlock = block;
                        hostPos = scanPos.Copy();
                        return hostPos;
                    }
                }
            }

            return null;
        }

        static bool RuleUsesTreeHost(Rule rule)
        {
            if (rule.HostKeys == null) return false;

            for (int i = 0; i < rule.HostKeys.Length; i++)
            {
                if (rule.HostKeys[i] == TreeHostToken) return true;
            }

            return false;
        }

        static int HorizontalChebyshev(BlockPos a, BlockPos b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dz = System.Math.Abs(a.Z - b.Z);
            return System.Math.Max(dx, dz);
        }

        static bool MatchesHostRule(Rule rule, Block block)
        {
            for (int i = 0; i < rule.HostKeys.Length; i++)
            {
                string key = rule.HostKeys[i];
                if (key == TreeHostToken && PlantCodeHelper.IsArborealHostBlock(block))
                    return true;

                string species = PlantCodeHelper.ResolveEcologySpecies(block);
                if (species != null && species == key)
                    return true;
            }
            return false;
        }

        /// <summary>Invalidate host cache entries near a removed host block.</summary>
        public static void InvalidateHostCacheAround(BlockPos pos, int radius)
        {
            if (pos == null || hostCache.Count == 0) return;

            int threshold = radius + 1;
            var toRemove = new List<long>();
            foreach (var kvp in hostCache)
            {
                BlockPos cached = kvp.Value;
                if (ReferenceEquals(cached, NoHost)) continue;
                if (HorizontalChebyshev(pos, cached) <= threshold)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                hostCache.Remove(toRemove[i]);
            }
        }

        static void StoreHostCache(long key, BlockPos value)
        {
            if (!hostCache.ContainsKey(key))
            {
                hostCacheOrder.Enqueue(key);
                while (hostCacheOrder.Count > MaxHostCacheEntries)
                {
                    long old = hostCacheOrder.Dequeue();
                    hostCache.Remove(old);
                }
            }
            hostCache[key] = value;
        }

        static long PosKey(BlockPos pos)
        {
            unchecked
            {
                return ((long)pos.X << 42) | ((long)(pos.Y & 0x1FFF) << 29) | (uint)pos.Z;
            }
        }
    }
}
