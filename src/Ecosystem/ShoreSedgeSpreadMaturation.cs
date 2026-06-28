using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    internal static class ShoreSedgeSpreadMaturation
    {
        static readonly HashSet<string> missingJuvenileWarnedSpecies = new HashSet<string>();

        public static Block ResolveSpreadBlock(
            ICoreAPI api,
            BlockPos origin,
            PlantRequirements requirements,
            Block matureSpreadBlock)
        {
            if (api == null || requirements == null || matureSpreadBlock == null) return matureSpreadBlock;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return matureSpreadBlock;
            if (!WildShoreSedgeEcology.IsSpecies(requirements.Species)) return matureSpreadBlock;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!WildFlowerMaturation.UsesMaturation(cfg, requirements.Species)) return matureSpreadBlock;

            AssetLocation juvenileCode = ShoreSedgeJuvenileBlocks.CodeForSpecies(requirements.Species);
            if (juvenileCode == null) return matureSpreadBlock;

            Block juvenile = api.World.GetBlock(juvenileCode);
            if (juvenile == null || juvenile.Id == 0)
            {
                WarnMissingJuvenileBlockOnce(api, cfg, requirements.Species, juvenileCode);
                return matureSpreadBlock;
            }

            return juvenile;
        }

        static void WarnMissingJuvenileBlockOnce(ICoreAPI api, EcosystemConfig cfg, string species, AssetLocation juvenileCode)
        {
            if (api == null || string.IsNullOrEmpty(species) || juvenileCode == null) return;
            if (!missingJuvenileWarnedSpecies.Add(species)) return;

            if (cfg != null && cfg.VerboseLogging)
            {
                api.Logger.Warning(
                    "[ecosystemflora] Juvenile sedge block missing: {0}; spread uses mature block",
                    juvenileCode);
                return;
            }

            api.Logger.Notification(
                "[ecosystemflora] Juvenile sedge block missing for {0} ({1}); spread uses mature block",
                species,
                juvenileCode);
        }

        public static bool ShouldQueueMaturation(Block placedBlock, PlantRequirements requirements)
        {
            if (placedBlock == null || requirements == null) return false;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            if (!WildShoreSedgeEcology.IsSpecies(requirements.Species)) return false;
            if (!WildFlowerMaturation.UsesMaturation(EcosystemConfig.Loaded, requirements.Species)) return false;
            return ShoreSedgeJuvenileBlocks.IsJuvenileBlock(placedBlock);
        }
    }
}
