using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Tallgrass spread offspring start at veryshort; mod raises height on a calendar timer.
    /// Ecology spread registration waits until height is at least short (not veryshort or eaten).
    /// </summary>
    internal static class TallgrassSpreadMaturation
    {
        public static bool UsesMaturation(EcosystemConfig cfg) =>
            cfg != null && cfg.EnableTallgrassSpreadMaturation;

        public static bool CanReproduceFrom(Block block)
        {
            if (block?.Code?.Path == null) return false;

            string path = block.Code.Path;
            if (path.StartsWith("tallgrass-eaten") || path.Contains("-eaten")) return false;

            if (!TallgrassSpreadHeight.TryParsePath(path, out TallgrassSpreadHeight.TallgrassPathParts parts))
            {
                return false;
            }

            if (string.IsNullOrEmpty(parts.Height)) return false;

            return TallgrassSpreadHeight.GetHeightStageIndex(parts.Height) >= 1;
        }

        public static bool ShouldQueuePromotion(Block block, PlantRequirements requirements)
        {
            if (block == null || requirements == null) return false;
            if (requirements.Species != "tallgrass") return false;
            if (!UsesMaturation(EcosystemConfig.Loaded)) return false;
            return !CanReproduceFrom(block);
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
