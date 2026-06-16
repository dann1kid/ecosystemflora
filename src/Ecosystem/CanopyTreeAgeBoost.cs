using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Older wild trees gain more spring branchy buds from calendar age at trunk base.</summary>
    internal static class CanopyTreeAgeBoost
    {
        public static float SpringBranchyBudMultiplier(ICoreAPI api, IBlockAccessor acc, BlockPos sourcePos, string wood)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSpringBranchyAgeBoost) return 1f;
            if (api == null || acc == null || sourcePos == null || string.IsNullOrEmpty(wood)) return 1f;

            int ageYears = ResolveTreeAgeYears(acc, sourcePos, wood);
            return SpringBranchyBudMultiplierForAge(ageYears);
        }

        internal static float SpringBranchyBudMultiplierForAge(int ageYears)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSpringBranchyAgeBoost || ageYears <= 0) return 1f;

            float yearsToMax = cfg.SpringBranchyAgeBoostYearsToMax > 0f
                ? cfg.SpringBranchyAgeBoostYearsToMax
                : 60f;
            float max = cfg.SpringBranchyAgeBoostMax > 1f ? cfg.SpringBranchyAgeBoostMax : 1.5f;

            float t = ageYears / yearsToMax;
            if (t > 1f) t = 1f;
            if (t < 0f) t = 0f;

            return 1f + t * (max - 1f);
        }

        internal static int ResolveTreeAgeYears(IBlockAccessor acc, BlockPos sourcePos, string wood)
        {
            BlockPos trunkBase = PlantCodeHelper.GetTreeTrunkBase(acc, sourcePos);
            if (EcosystemSystem.Instance?.TryGetReproducer(trunkBase, out ReproducerEntry entry) == true)
            {
                return entry.TreeAgeYears < 0 ? 0 : entry.TreeAgeYears;
            }

            return 0;
        }
    }
}
