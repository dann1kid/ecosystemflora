using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

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

            if (SpeciesEcologyRegistry.IsLoaded && SpeciesEcologyRegistry.TryGet(requirements.Species, out SpeciesEcologyCsvRow row))
            {
                return row.Taxon == "berry";
            }

            return EcologyBerrySpecies.IsKnown(requirements.Species);
        }

        public static double MaturationHours(ICoreAPI api, BlockPos pos, string species, EcosystemConfig cfg)
        {
            double hours;
            if (SpeciesEcologyRegistry.IsLoaded
                && SpeciesEcologyRegistry.TryGetBerryMaturationHours(species, out double csvHours))
            {
                hours = csvHours * (cfg?.GrowthHoursMultiplier ?? 1f);
            }
            else if (!SpeciesEcologyLegacyAccess.TryGetBerrySpreadRate(species, out float spreadRate))
            {
                hours = DefaultMaturationHours * (cfg?.GrowthHoursMultiplier ?? 1f);
            }
            else
            {
                hours = DefaultMaturationHours / System.Math.Max(
                    0.25f,
                    WildSpreadBalance.ScaleSpeciesSpreadRate(species, spreadRate, cfg));
                hours *= cfg?.GrowthHoursMultiplier ?? 1f;
            }

            return CalendarSpeedHelper.ScaleCalendarHours(hours, api?.World?.Calendar);
        }
    }
}

