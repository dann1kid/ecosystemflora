using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class BerrySpreadMaturation
    {
        /// <summary>Base calendar hours before a spread berry bush enters the reproduce registry (~7 game-days at SpreadRate 0.5).</summary>
        const double DefaultMaturationHours = 168;

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
