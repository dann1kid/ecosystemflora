using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Once per game year, advance calendar tree age and maturate registered trunks (same scope as spread).</summary>
    internal sealed class TreeGrowthScheduler
    {
        int lastProcessedYear = int.MinValue;
        int roundRobinIndex;
        readonly List<TreeSenescence.PendingRemoval> pendingSenescence = new List<TreeSenescence.PendingRemoval>();

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks,
            TreeCalendarAgeStore calendarAgeStore = null,
            System.Action<TreeSenescence.PendingRemoval> onSenescenceRemoved = null)
        {
            pendingSenescence.Clear();
            if (!cfg.EnableTreeAging || api?.World?.BlockAccessor == null || registry == null) return;

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
            float scale = cfg.TreeGrowthActivityScale;

            for (int n = 0; n < attempts; n++)
            {
                if (roundRobinIndex >= totalEntries)
                {
                    roundRobinIndex = 0;
                }

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null) continue;
                if (entry.Requirements?.Habitat != EcologyHabitat.TerrestrialTree) continue;
                if (entry.LastTreeGrowthYear >= gameYear) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                Block block = acc.GetBlock(entry.Origin);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) continue;

                string wood = PlantCodeHelper.GetTreeWood(block);
                if (string.IsNullOrEmpty(wood) || !WildTreeEcology.TryGet(wood, out _)) continue;

                entry.TreeAgeYears++;
                if (entry.TreeAgeYears < 0) entry.TreeAgeYears = 0;

                WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
                if (TreeSenescence.IsSenescent(entry.TreeAgeYears, profile, cfg))
                {
                    int removed = TreeSenescence.RemoveWholeTree(api, acc, entry.Origin, wood);
                    if (removed > 0)
                    {
                        pendingSenescence.Add(new TreeSenescence.PendingRemoval(
                            entry.Origin.Copy(),
                            wood,
                            removed));

                        if (cfg.ReproduceDebug)
                        {
                            api.Logger.Notification(
                                "[ecosystemflora] Tree senescence death {0}y ({1}): removed {2} block(s) at {3}",
                                entry.TreeAgeYears,
                                wood,
                                removed,
                                entry.Origin);
                        }
                    }
                    else
                    {
                        entry.LastTreeGrowthYear = gameYear;
                        calendarAgeStore?.Capture(entry, wood);
                    }

                    continue;
                }

                int placed = TreeGrowthApplier.TryGrowYear(
                    api,
                    acc,
                    entry.Origin,
                    wood,
                    gameYear,
                    scale);

                entry.LastTreeGrowthYear = gameYear;
                calendarAgeStore?.Capture(entry, wood);

                if (placed > 0 && cfg.ReproduceDebug)
                {
                    TreeStructureMetrics metrics = TreeStructureProbe.Measure(acc, entry.Origin, wood);
                    int sizePct = TreeGrowthTargets.SizeIndexPercent(
                        metrics.TrunkHeight,
                        metrics.CrownRadius,
                        profile);

                    api.Logger.Notification(
                        "[ecosystemflora] Tree {0}y ({1}) size {2}%: +{3} block(s) at {4}",
                        entry.TreeAgeYears,
                        wood,
                        sizePct,
                        placed,
                        entry.Origin);
                }
            }

            if (onSenescenceRemoved != null)
            {
                for (int i = 0; i < pendingSenescence.Count; i++)
                {
                    onSenescenceRemoved(pendingSenescence[i]);
                }
            }
        }

        public void Clear()
        {
            lastProcessedYear = int.MinValue;
            roundRobinIndex = 0;
            pendingSenescence.Clear();
        }
    }
}
