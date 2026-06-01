using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class WildSoilGroundRules
    {
        /// <summary>Lake ice, glacier ice, snow — not valid wild plant footing (solid or fluid layer).</summary>
        public static bool IsUnplantableGround(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (BlockFluidHelper.IsFluid(block)) return true;

            string path = block.Code?.Path;
            if (IsUnplantableGroundPath(path)) return true;

            return block.BlockMaterial == EnumBlockMaterial.Snow;
        }

        internal static bool IsUnplantableGroundPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.Contains("ice", StringComparison.OrdinalIgnoreCase)
                || path.Contains("snow", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFarmland(Block ground)
        {
            if (ground?.Code == null || ground.Id == 0) return false;
            string path = ground.Code.Path;
            return !string.IsNullOrEmpty(path) && path.StartsWith("farmland");
        }

        /// <summary>
        /// True when vanilla mushroom regrowth has an active <c>Mycelium</c> block entity here.
        /// Do not use <see cref="BlockBehaviorMyceliumHost"/> on the block type — most soils carry that behavior.
        /// </summary>
        public static bool HasActiveMycelium(IBlockAccessor acc, BlockPos groundPos)
        {
            if (acc == null || groundPos == null) return false;

            BlockEntity be = acc.GetBlockEntity(groundPos);
            if (be == null) return false;

            string typeName = be.GetType().Name;
            return typeName == "BlockEntityMycelium" || typeName.Contains("Mycelium");
        }

        public static bool IsWildSpreadGround(Block ground)
        {
            if (ground == null || ground.Id == 0) return false;
            if (IsFarmland(ground)) return false;
            return WildSoilBlockMapper.IsSuccessionTarget(ground);
        }
    }
}
