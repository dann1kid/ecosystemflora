using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class BerrySpreadMaturation
    {
        const double DefaultMaturationHours = 96;

        public static bool UsesMaturation(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableBerrySpreadMaturation && cfg.EcosystemEnabled;

        public static bool ShouldQueueMaturation(
            Block placed,
            PlantRequirements requirements,
            ICoreAPI api,
            BlockPos pos)
        {
            if (!UsesMaturation(EcosystemConfig.Loaded)) return false;
            if (requirements == null || placed == null || placed.Id == 0) return false;
            return WildBerryEcology.TryGet(requirements.Species, out _);
        }

        public static double MaturationHours(ICoreAPI api, BlockPos pos, string species, EcosystemConfig cfg)
        {
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile profile))
            {
                return DefaultMaturationHours * (cfg?.GrowthHoursMultiplier ?? 1f);
            }

            double baseHours = DefaultMaturationHours / System.Math.Max(0.25f, profile.SpreadRate);
            return baseHours * (cfg?.GrowthHoursMultiplier ?? 1f);
        }
    }
}
