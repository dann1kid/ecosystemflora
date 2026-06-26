using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class FernSpreadMaturation
    {
        static readonly HashSet<string> missingJuvenileWarnedSpecies = new HashSet<string>();

        public static Block ResolveSpreadBlock(
            ICoreAPI api,
            BlockPos origin,
            PlantRequirements requirements,
            Block matureSpreadBlock)
        {
            if (api == null || requirements == null || matureSpreadBlock == null) return matureSpreadBlock;
            if (!WildFernSpread.UsesMaturation(EcosystemConfig.Loaded, requirements.Species)) return matureSpreadBlock;

            AssetLocation juvenileCode = FernJuvenileBlocks.CodeForSpecies(requirements.Species);
            if (juvenileCode == null) return matureSpreadBlock;

            Block juvenile = api.World.GetBlock(juvenileCode);
            if (juvenile == null || juvenile.Id == 0)
            {
                WarnMissingJuvenileBlockOnce(api, requirements.Species, juvenileCode);
                return matureSpreadBlock;
            }

            return juvenile;
        }

        static void WarnMissingJuvenileBlockOnce(ICoreAPI api, string species, AssetLocation juvenileCode)
        {
            if (api == null || string.IsNullOrEmpty(species) || juvenileCode == null) return;
            if (!missingJuvenileWarnedSpecies.Add(species)) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg != null && cfg.VerboseLogging)
            {
                api.Logger.Warning(
                    "[ecosystemflora] Juvenile fern block missing: {0}; spread uses mature block",
                    juvenileCode);
                return;
            }

            api.Logger.Notification(
                "[ecosystemflora] Juvenile fern block missing for {0} ({1}); spread uses mature block",
                species,
                juvenileCode);
        }

        public static bool ShouldQueueMaturation(Block placedBlock, PlantRequirements requirements)
        {
            if (placedBlock == null || requirements == null) return false;
            if (!WildFernSpread.UsesMaturation(EcosystemConfig.Loaded, requirements.Species)) return false;
            return FernJuvenileBlocks.IsJuvenileBlock(placedBlock);
        }
    }
}
