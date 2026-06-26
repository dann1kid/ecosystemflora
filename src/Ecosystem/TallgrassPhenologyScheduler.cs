using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class TallgrassPhenologyScheduler
    {
        int roundRobinIndex;

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks,
            double nowHours)
        {
            if (!cfg.EnableTallgrassPhenology || api == null || registry == null) return;
            if (cfg.MaxTallgrassPhenologyChecksPerTick <= 0) return;

            int totalEntries = registry.Count;
            if (totalEntries == 0) return;

            int attempts = cfg.MaxTallgrassPhenologyChecksPerTick;
            for (int n = 0; n < attempts; n++)
            {
                if (roundRobinIndex >= totalEntries)
                {
                    roundRobinIndex = 0;
                }

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null || !TallgrassPhenology.UsesPhenology(cfg, entry.Requirements)) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
                if (!TallgrassPhenology.IsRegisteredPlantBlock(entry, block)) continue;

                TallgrassPhenology.Advance(api, entry, cfg, nowHours);
            }
        }
    }
}
