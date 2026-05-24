using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Symbionts die when required host vanishes nearby.</summary>
    internal static class FloraSymbiosis
    {
        public const string TreeHostToken = "tree";

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

            return FindHost(acc, symbiontPos, rule, out _, out _) != null;
        }

        public static void CascadeOnHostRemoved(ICoreAPI api, BlockPos hostPos, Block hostBlock)
        {
            if (api == null || hostPos == null || !EcosystemConfig.Loaded.EnableSymbiosis) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            int radius = EcosystemConfig.Loaded.SymbiosisCascadeRadius;
            IBlockAccessor acc = api.World.BlockAccessor;
            var scanPos = new BlockPos();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        scanPos.Set(hostPos.X + dx, hostPos.Y + dy, hostPos.Z + dz);
                        if (scanPos.Equals(hostPos)) continue;

                        Block block = acc.GetBlock(scanPos);
                        if (!PlantCodeHelper.IsEcologySpreadParent(block)) continue;

                        string symbiontSpecies = PlantCodeHelper.GetEcologySpecies(block.Code);
                        if (!TryGetRule(symbiontSpecies, out Rule rule)) continue;

                        if (!HostMatchesRemoved(rule, hostBlock, hostPos, scanPos)) continue;

                        eco.RemoveEcologyPlant(scanPos, cascadeSymbiosis: false, reason: "symbiosis");
                    }
                }
            }
        }

        static bool HostMatchesRemoved(Rule rule, Block hostBlock, BlockPos hostPos, BlockPos symbiontPos)
        {
            if (hostBlock == null) return false;

            int dist = HorizontalChebyshev(hostPos, symbiontPos);
            if (dist > rule.MaxHostDistance) return false;

            for (int i = 0; i < rule.HostKeys.Length; i++)
            {
                string key = rule.HostKeys[i];
                if (key == TreeHostToken && PlantCodeHelper.IsTreeLogGrownBlock(hostBlock))
                {
                    return true;
                }

                string hostSpecies = PlantCodeHelper.GetEcologySpecies(hostBlock.Code);
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

            var scanPos = new BlockPos();
            for (int dx = -rule.MaxHostDistance; dx <= rule.MaxHostDistance; dx++)
            {
                for (int dz = -rule.MaxHostDistance; dz <= rule.MaxHostDistance; dz++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        scanPos.Set(symbiontPos.X + dx, symbiontPos.Y + dy, symbiontPos.Z + dz);
                        if (scanPos.Equals(symbiontPos)) continue;

                        Block block = acc.GetBlock(scanPos);
                        if (block.Id == 0) continue;

                        if (HostMatchesRemoved(rule, block, scanPos, symbiontPos))
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

        static int HorizontalChebyshev(BlockPos a, BlockPos b)
        {
            int dx = System.Math.Abs(a.X - b.X);
            int dz = System.Math.Abs(a.Z - b.Z);
            return System.Math.Max(dx, dz);
        }
    }
}
