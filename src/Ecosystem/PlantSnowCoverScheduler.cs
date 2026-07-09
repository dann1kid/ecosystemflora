using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Round-robin <c>-free</c> / <c>-snow</c> sync for registered plants.
    /// Mod phase blocks do not self-update like vanilla frostable tallplants.
    /// </summary>
    internal sealed class PlantSnowCoverScheduler
    {
        const int DefaultChecksPerTick = 96;

        int roundRobinIndex;

        public void Tick(
            ICoreAPI api,
            EcosystemConfig cfg,
            ReproducerRegistry registry,
            ICollection<Vec2i> activeChunks)
        {
            if (!cfg.EcosystemEnabled || api == null || registry == null) return;

            int budget = DefaultChecksPerTick;
            if (budget <= 0) return;

            int totalEntries = registry.Count;
            if (totalEntries == 0) return;

            for (int n = 0; n < budget; n++)
            {
                if (roundRobinIndex >= totalEntries)
                {
                    roundRobinIndex = 0;
                }

                ReproducerEntry entry = registry.GetEntry(roundRobinIndex++);
                if (entry == null) continue;
                if (cfg.OnlyActivateNearPlayers
                    && !ReproducerRegistry.IsInActiveChunks(entry.Origin, activeChunks))
                {
                    continue;
                }

                Block block = api.World.BlockAccessor.GetBlock(entry.Origin);
                PlantSnowCoverSync.TrySyncCover(api, entry.Origin, block);
            }
        }
    }
}
