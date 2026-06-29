using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Guesses calendar age when a trunk is first registered without persisted age data.</summary>
    internal static class TreeRegistrationAge
    {
        public static int EstimateFromStructure(IBlockAccessor acc, BlockPos trunkBase, string wood)
        {
            if (acc == null || trunkBase == null || string.IsNullOrEmpty(wood)) return 0;

            TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, trunkBase, wood);
            WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
            int sizePct = TreeGrowthTargets.SizeIndexPercent(
                metrics.TrunkHeight,
                metrics.CrownRadius,
                profile);

            if (sizePct <= 5) return 0;

            if (profile.SpreadMaturityAgeYears > 0)
            {
                float t = (sizePct - 5f) / 75f;
                t = GameMath.Clamp(t, 0f, 1f);
                return (int)System.Math.Round(t * profile.SpreadMaturityAgeYears);
            }

            return System.Math.Max(0, sizePct / 8);
        }
    }
}
