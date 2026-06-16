using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Symbionts die when required host vanishes nearby.</summary>
    internal static class FloraSymbiosis
    {
        public const string TreeHostToken = "tree";

        /// <summary>Ground symbionts can sit many blocks below canopy logs.</summary>
        const int TreeHostVerticalSearchUp = 16;
        const int TreeCascadeVerticalSearchDown = 16;
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
                ["hartstongue"] = new Rule(new[] { TreeHostToken }, 2),
                ["tallfern"] = new Rule(new[] { TreeHostToken }, 2),
                ["blueberry"] = new Rule(new[] { TreeHostToken }, 2),
                ["blackcurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["redcurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["whitecurrant"] = new Rule(new[] { TreeHostToken }, 4),
                ["cranberry"] = new Rule(new[] { TreeHostToken }, 3),
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

        public static void CascadeOnHostRemoved(ICoreAPI api, BlockPos hostPos, Block hostBlock)
        {
            if (api == null || hostPos == null || !EcosystemConfig.Loaded.EnableSymbiosis) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            int radius = EcosystemConfig.Loaded.SymbiosisCascadeRadius;
            InvalidateHostCacheAround(hostPos, radius);
            IBlockAccessor acc = api.World.BlockAccessor;
            bool treeHost = PlantCodeHelper.IsArborealHostBlock(hostBlock);
            int scanDown = treeHost ? TreeCascadeVerticalSearchDown : NonTreeVerticalSearch;
            int scanUp = NonTreeVerticalSearch;
            var scanPos = new BlockPos(0);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -scanDown; dy <= scanUp; dy++)
                    {
                        scanPos.Set(hostPos.X + dx, hostPos.Y + dy, hostPos.Z + dz);
                        if (scanPos.Equals(hostPos)) continue;

                        Block block = acc.GetBlock(scanPos);
                        if (!PlantCodeHelper.IsEcologySpreadParent(block)) continue;

                        string symbiontSpecies = PlantCodeHelper.ResolveEcologySpecies(block);
                        if (!TryGetRule(symbiontSpecies, out Rule rule)) continue;

                        if (!SymbiontLinkedToRemovedHost(rule, hostBlock, hostPos, scanPos)) continue;

                        eco.RemoveEcologyPlant(scanPos, cascadeSymbiosis: false, reason: "symbiosis");
                    }
                }
            }
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
