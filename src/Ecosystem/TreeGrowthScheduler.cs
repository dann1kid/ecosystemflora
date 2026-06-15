using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Once per game year, advance calendar tree age and maturate near active players.</summary>
    internal sealed class TreeGrowthScheduler
    {
        int lastProcessedYear = int.MinValue;
        int roundRobinIndex;

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks)
        {
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

                if (cfg.OnlyActivateNearPlayers)
                {
                    if (activeChunks == null || activeChunks.Count == 0) continue;

                    int cs = GlobalConstants.ChunkSize;
                    var chunk = new Vec2i(
                        entry.Origin.X / cs,
                        entry.Origin.Z / cs);
                    bool nearPlayer = false;
                    foreach (Vec2i active in activeChunks)
                    {
                        if (active.X == chunk.X && active.Y == chunk.Y)
                        {
                            nearPlayer = true;
                            break;
                        }
                    }

                    if (!nearPlayer) continue;
                }

                Block block = acc.GetBlock(entry.Origin);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(block)) continue;

                string wood = PlantCodeHelper.GetTreeWood(block);
                if (string.IsNullOrEmpty(wood) || !WildTreeEcology.TryGet(wood, out _)) continue;

                entry.TreeAgeYears++;
                if (entry.TreeAgeYears < 0) entry.TreeAgeYears = 0;

                int placed = TreeGrowthApplier.TryGrowYear(
                    api,
                    acc,
                    entry.Origin,
                    wood,
                    gameYear,
                    scale);

                entry.LastTreeGrowthYear = gameYear;

                if (placed > 0 && cfg.ReproduceDebug)
                {
                    WildTreeGrowthProfiles.Profile profile = WildTreeGrowthProfiles.Resolve(wood);
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
        }

        public void Clear()
        {
            lastProcessedYear = int.MinValue;
            roundRobinIndex = 0;
        }
    }
}
