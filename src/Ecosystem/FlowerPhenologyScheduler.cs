using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Round-robin phenology advance for registered meadow flowers.</summary>
    internal sealed class FlowerPhenologyScheduler
    {
        int roundRobinIndex;

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks,
            double nowHours)
        {
            if (!cfg.EnableFlowerPhenology || api == null || registry == null) return;
            if (cfg.MaxFlowerPhenologyChecksPerTick <= 0) return;

            int totalEntries = registry.Count;
            if (totalEntries == 0) return;

            int attempts = cfg.MaxFlowerPhenologyChecksPerTick;
            for (int n = 0; n < attempts; n++)
            {
                if (roundRobinIndex >= totalEntries)
                {
                    roundRobinIndex = 0;
                }

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null || !FlowerPhenology.UsesPhenology(cfg, entry.Requirements)) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
                if (!FlowerPhenology.IsRegisteredPlantBlock(entry, block)) continue;

                FlowerPhenology.Advance(api, entry, cfg, nowHours);
            }
        }
    }
}
