using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Tallgrass below environment target height: staged growth on main thread, then registry insert.
    /// </summary>
    internal static class TallgrassEstablishment
    {
        public static bool UsesEstablishment(EcosystemConfig cfg) =>
            TallgrassSpreadMaturation.UsesMaturation(cfg);

        public static bool NeedsEstablishment(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            out int targetStageIndex,
            PlantRequirements requirements = null)
        {
            targetStageIndex = -1;
            if (!UsesEstablishment(EcosystemConfig.Loaded) || block?.Code?.Path == null) return false;

            if (requirements == null)
            {
                if (PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass") return false;
            }
            else if (requirements.Species != "tallgrass")
            {
                return false;
            }

            if (!TallgrassSpreadHeight.TryParsePath(block.Code.Path, out TallgrassSpreadHeight.TallgrassPathParts parts))
            {
                return false;
            }

            if (string.IsNullOrEmpty(parts.Height)) return false;
            if (block.Code.Path.StartsWith("tallgrass-eaten") || block.Code.Path.Contains("-eaten"))
            {
                return false;
            }

            int currentIdx = TallgrassSpreadHeight.GetHeightStageIndex(parts.Height);
            if (currentIdx < 0) return false;

            requirements ??= PlantRequirements.FromBlock(block);
            targetStageIndex = TallgrassSpreadHeight.PickTargetStageIndex(api, pos, requirements);
            return currentIdx < targetStageIndex;
        }

        /// <summary>Cheap gate before <see cref="PlantRequirements.FromBlock"/> on block placement / scan miss paths.</summary>
        public static bool ShouldQueueAfterPlacement(ICoreAPI api, BlockPos pos, Block block)
        {
            if (!UsesEstablishment(EcosystemConfig.Loaded) || block?.Code?.Path == null) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass") return false;

            PlantRequirements requirements = PlantRequirements.FromBlock(block);
            return NeedsEstablishment(api, pos, block, out _, requirements);
        }

        public static bool ShouldQueueEstablishment(
            ICoreAPI api,
            BlockPos pos,
            Block block,
            PlantRequirements requirements)
        {
            if (requirements == null) return false;
            return NeedsEstablishment(api, pos, block, out _, requirements);
        }

        public static bool IsReadyToRegister(
            Block block,
            int targetStageIndex,
            ICoreAPI api = null,
            BlockPos pos = null)
        {
            if (block == null || targetStageIndex < 0) return false;

            if (!TallgrassSpreadHeight.TryParsePath(block.Code.Path, out TallgrassSpreadHeight.TallgrassPathParts parts))
            {
                return false;
            }

            int currentIdx = TallgrassSpreadHeight.GetHeightStageIndex(parts.Height);
            if (currentIdx < 0) return false;

            int minSpreadIdx = TallgrassSpreadHeight.MinSpreadStageIndex(targetStageIndex);
            return currentIdx >= minSpreadIdx;
        }
    }
}

