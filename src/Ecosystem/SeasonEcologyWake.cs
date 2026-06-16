using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Coarse calendar wake when monthly spread curves shift (Phase 6.6).</summary>
    internal static class SeasonEcologyWake
    {
        public static bool ShouldWakeEntry(ReproducerEntry entry)
        {
            if (entry?.Requirements == null) return false;

            switch (entry.Requirements.Habitat)
            {
                case EcologyHabitat.MyceliumAnchor:
                case EcologyHabitat.WildVine:
                    return false;
            }

            return WildSpeciesSeason.UsesSeasonalSpread(entry.Requirements.Species);
        }

        public static void TryWakeOnMonthChange(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ref int lastWakeMonth)
        {
            if (api?.World?.Calendar == null || cfg == null || registry == null) return;
            if (!cfg.EnableSeasonCoarseWake || !cfg.UseSeasonalEcology || !cfg.EnableEventDrivenSpread) return;

            int month = api.World.Calendar.Month;
            if (month == lastWakeMonth) return;

            lastWakeMonth = month;
            registry.WakeMatching(ShouldWakeEntry);
        }
    }
}
