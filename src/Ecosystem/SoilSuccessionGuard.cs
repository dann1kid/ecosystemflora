using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Skip ground block swaps when slabs or builds sit on the soil column.</summary>
    internal static class SoilSuccessionGuard
    {
        public static bool CanModifyGroundBelow(IBlockAccessor acc, BlockPos groundPos)
        {
            if (acc == null || groundPos == null) return false;

            Block ground = acc.GetBlock(groundPos);
            if (IsSlabProtectedBlock(ground)) return false;

            Block above = acc.GetBlock(groundPos.UpCopy());
            if (above == null || above.Id == 0) return true;

            if (IsSlabProtectedBlock(above)) return false;

            if (PlantCodeHelper.IsEcologyPlant(above)) return true;
            if (above.BlockMaterial == EnumBlockMaterial.Plant) return true;
            if (above.Replaceable >= 3000) return true;

            return false;
        }

        /// <summary>Terrain Slabs replaces ground with slab-shaped soil; player slabs use a *slab* path.</summary>
        internal static bool IsSlabProtectedBlock(Block block)
        {
            if (block == null || block.Id == 0 || block.Code == null) return false;

            return IsSlabProtectedBlock(block.Code.Domain, block.Code.Path);
        }

        internal static bool IsSlabProtectedBlock(string domain, string path)
        {
            if (!string.IsNullOrEmpty(path)
                && path.IndexOf("slab", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (string.Equals(domain, "terrainslabs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
