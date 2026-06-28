using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    /// <summary>Once per game year: ferntree calendar age, growth, and senescence.</summary>
    internal sealed class FerntreeGrowthScheduler
    {
        int lastProcessedYear = int.MinValue;
        int roundRobinIndex;
        readonly List<TreeSenescence.PendingRemoval> pendingRemovals = new List<TreeSenescence.PendingRemoval>();

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks,
            TreeCalendarAgeStore calendarAgeStore = null,
            System.Action<TreeSenescence.PendingRemoval> onRemoved = null)
        {
            pendingRemovals.Clear();
            if (!cfg.EnableFerntreeEcology || !cfg.EnableTreeAging || api?.World?.BlockAccessor == null || registry == null)
            {
                return;
            }

            IGameCalendar cal = api.World.Calendar;
            if (cal == null || cal.DaysPerYear <= 0) return;

            int gameYear = CanopyEcology.GameYear(cal);
            if (gameYear != lastProcessedYear)
            {
                lastProcessedYear = gameYear;
                roundRobinIndex = 0;
            }

            if (cfg.MaxTreeGrowthAttemptsPerTick <= 0) return;

            int attempts = cfg.MaxTreeGrowthAttemptsPerTick;
            int totalEntries = registry.Count;
            if (totalEntries == 0) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            WildFerntreeEcology.Profile profile = WildFerntreeEcology.Resolve();

            for (int n = 0; n < attempts; n++)
            {
                if (roundRobinIndex >= totalEntries) roundRobinIndex = 0;

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null) continue;
                if (entry.Requirements?.Habitat != EcologyHabitat.Ferntree) continue;
                if (entry.LastTreeGrowthYear >= gameYear) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                if (!FerntreeStructure.IsTrunkBlock(acc.GetBlock(entry.Origin))) continue;

                entry.TreeAgeYears++;
                if (entry.TreeAgeYears < 0) entry.TreeAgeYears = 0;

                if (FerntreeSenescence.IsPastHorizon(entry.TreeAgeYears, profile, cfg))
                {
                    if (cfg.EnableTreeSenescence)
                    {
                        TreeSenescence.YearAdvanceResult result = FerntreeSenescence.AdvanceSenescenceYear(
                            api,
                            acc,
                            entry.Origin,
                            entry.TreeSenescencePhase,
                            cfg);

                        if (result.NewPhase != entry.TreeSenescencePhase
                            || result.BlocksRemoved > 0
                            || result.Completed)
                        {
                            entry.TreeSenescencePhase = result.NewPhase;
                            if (result.Completed)
                            {
                                pendingRemovals.Add(result.Removal);
                            }
                        }
                    }

                    entry.LastTreeGrowthYear = gameYear;
                    calendarAgeStore?.Capture(entry, WildFerntreeEcology.Species);
                    continue;
                }

                int changed = 0;
                FerntreeTopMaturity maturity = FerntreeStructure.MaturityForAge(entry.TreeAgeYears, profile);
                if (FerntreeStructure.TrySetTopMaturity(acc, entry.Origin, maturity))
                {
                    changed++;
                }

                if (entry.TreeAgeYears % 3 == 0
                    && FerntreeStructure.TryGrowOneSegment(acc, entry.Origin, profile.MaxTrunkHeight))
                {
                    changed++;
                    FerntreeStructure.TrySetTopMaturity(acc, entry.Origin, maturity);
                }

                entry.LastTreeGrowthYear = gameYear;
                calendarAgeStore?.Capture(entry, WildFerntreeEcology.Species);

                if (changed > 0 && cfg.ReproduceDebug)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] Ferntree {0}y: structure update at {1}",
                        entry.TreeAgeYears,
                        entry.Origin);
                }
            }

            if (onRemoved != null)
            {
                for (int i = 0; i < pendingRemovals.Count; i++)
                {
                    onRemoved(pendingRemovals[i]);
                }
            }
        }

        public void Clear()
        {
            lastProcessedYear = int.MinValue;
            roundRobinIndex = 0;
            pendingRemovals.Clear();
        }
    }
}
