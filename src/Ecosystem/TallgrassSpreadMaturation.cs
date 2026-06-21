using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Tallgrass spread offspring start at veryshort; mod raises height on a calendar timer.
    /// Spread registration opens at half the environment target height; promotion continues to full target.
    /// </summary>
    internal static class TallgrassSpreadMaturation
    {
        public static bool UsesMaturation(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableTallgrassSpreadMaturation;

        public static bool CanReproduceFrom(Block block, ICoreAPI api = null, BlockPos pos = null)
        {
            if (block?.Code?.Path == null) return false;

            string path = block.Code.Path;
            if (path.StartsWith("tallgrass-eaten") || path.Contains("-eaten")) return false;

            if (!TallgrassSpreadHeight.TryParsePath(path, out TallgrassSpreadHeight.TallgrassPathParts parts))
            {
                return false;
            }

            if (string.IsNullOrEmpty(parts.Height)) return false;

            int currentIdx = TallgrassSpreadHeight.GetHeightStageIndex(parts.Height);
            if (currentIdx < 0) return false;

            PlantRequirements requirements = PlantRequirements.FromBlock(block);
            int targetIdx = TallgrassSpreadHeight.PickTargetStageIndex(api, pos, requirements);
            int minSpreadIdx = TallgrassSpreadHeight.MinSpreadStageIndex(targetIdx);
            return currentIdx >= minSpreadIdx;
        }

        public static bool ShouldQueuePromotion(
            Block block,
            PlantRequirements requirements,
            ICoreAPI api = null,
            BlockPos pos = null)
        {
            if (block == null || requirements == null) return false;
            return TallgrassEstablishment.ShouldQueueEstablishment(api, pos, block, requirements);
        }

        public static Block ResolveSpreadBlock(
            ICoreAPI api,
            BlockPos plantPos,
            Block parentBlock,
            PlantRequirements requirements,
            System.Random rand)
        {
            if (!UsesMaturation(EcosystemConfig.Loaded))
            {
                return TallgrassSpreadHeight.ResolveSpreadBlock(api, plantPos, parentBlock, requirements, rand);
            }

            return TallgrassSpreadHeight.ResolveVeryshortSpreadBlock(api, parentBlock);
        }
    }

}
