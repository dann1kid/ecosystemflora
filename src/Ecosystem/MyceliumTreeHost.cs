using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal enum MyceliumTreeHostKind
    {
        Any = 0,
        Deciduous = 1,
        Conifer = 2,
    }

    /// <summary>Tree host lookup for mycelium anchor survival.</summary>
    internal static class MyceliumTreeHost
    {
        static readonly HashSet<string> ConiferWoods = BuildWoodSet(
            "pine", "larch", "redwood", "baldcypress", "greenspirecypress");

        static readonly HashSet<string> DeciduousWoods = BuildWoodSet(
            "birch", "oak", "maple", "crimsonkingmaple", "walnut", "kapok",
            "ebony", "purpleheart", "acacia");

        static HashSet<string> BuildWoodSet(params string[] woods)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string wood in woods)
            {
                if (!string.IsNullOrEmpty(wood)) set.Add(wood);
            }

            return set;
        }

        public static bool IsDeciduousWood(string wood) =>
            !string.IsNullOrEmpty(wood) && DeciduousWoods.Contains(wood);

        public static bool HasHostInRange(
            IBlockAccessor acc,
            BlockPos anchorPos,
            int radius,
            MyceliumTreeHostKind kind)
        {
            if (acc == null || anchorPos == null || radius < 0) return false;

            var scan = new BlockPos(anchorPos.dimension);
            int r = radius;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dz)) > r) continue;

                    for (int dy = -2; dy <= 16; dy++)
                    {
                        scan.Set(anchorPos.X + dx, anchorPos.Y + dy, anchorPos.Z + dz);
                        if (TryMatchHost(acc.GetBlock(scan), kind)) return true;
                    }
                }
            }

            return false;
        }

        static bool TryMatchHost(Block block, MyceliumTreeHostKind kind)
        {
            if (!FloraContextSampler.IsForestNeighborBlock(block)) return false;

            string wood = PlantCodeHelper.GetTreeWood(block);
            if (string.IsNullOrEmpty(wood))
            {
                return kind == MyceliumTreeHostKind.Any;
            }

            switch (kind)
            {
                case MyceliumTreeHostKind.Deciduous:
                    return DeciduousWoods.Contains(wood);
                case MyceliumTreeHostKind.Conifer:
                    return ConiferWoods.Contains(wood);
                default:
                    return DeciduousWoods.Contains(wood) || ConiferWoods.Contains(wood)
                        || WildTreeEcology.TryGet(wood, out _);
            }
        }

        public static MyceliumTreeHostKind HostKindForNiche(MyceliumNiche niche)
        {
            switch (niche)
            {
                case MyceliumNiche.ForestDeciduous:
                    return MyceliumTreeHostKind.Deciduous;
                case MyceliumNiche.ForestConifer:
                    return MyceliumTreeHostKind.Conifer;
                case MyceliumNiche.ForestAnyTree:
                case MyceliumNiche.TrunkPolypore:
                    return MyceliumTreeHostKind.Any;
                default:
                    return MyceliumTreeHostKind.Any;
            }
        }
    }
}
