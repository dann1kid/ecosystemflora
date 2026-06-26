using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Round-robin fern phenology advance for registered ferns.</summary>
    internal sealed class FernPhenologyScheduler
    {
        int roundRobinIndex;

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks,
            double nowHours)
        {
            if (!cfg.EnableFernPhenology || api == null || registry == null) return;
            if (cfg.MaxFernPhenologyChecksPerTick <= 0) return;

            int totalEntries = registry.Count;
            if (totalEntries == 0) return;

            int attempts = cfg.MaxFernPhenologyChecksPerTick;
            for (int n = 0; n < attempts; n++)
            {
                if (roundRobinIndex >= totalEntries)
                {
                    roundRobinIndex = 0;
                }

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null || !FernPhenology.UsesPhenology(cfg, entry.Requirements)) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
                if (!FernPhenology.IsRegisteredPlantBlock(entry, block)) continue;

                FernPhenology.Advance(api, entry, cfg, nowHours);
            }
        }
    }
}
