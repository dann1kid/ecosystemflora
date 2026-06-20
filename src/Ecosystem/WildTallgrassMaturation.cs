using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Calendar timing for tallgrass height stages when spread places veryshort.</summary>
    internal static class WildTallgrassMaturation
    {
        const double VeryshortToShortHours = 36;

        public static double StageAdvanceHours(ICoreAPI api, BlockPos pos, EcosystemConfig cfg)
        {
            double hours = VeryshortToShortHours;
            if (cfg != null && cfg.GrowthHoursMultiplier > 0.05f)
            {
                hours /= cfg.GrowthHoursMultiplier;
            }

            if (cfg != null && cfg.UseSeasonalEcology && api != null && pos != null)
            {
                var req = new PlantRequirements { Species = "tallgrass" };
                float season = SeasonEcology.SpreadActivityMultiplier(api, pos, req);
                if (season > 0.05f)
                {
                    hours /= System.Math.Min(season, 2f);
                }
            }

            if (hours < 6) hours = 6;
            return hours;
        }
    }
}
