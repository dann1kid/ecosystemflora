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

            Block above = acc.GetBlock(groundPos.UpCopy());
            if (above == null || above.Id == 0) return true;

            if (PlantCodeHelper.IsEcologyPlant(above)) return true;
            if (above.BlockMaterial == EnumBlockMaterial.Plant) return true;
            if (above.Replaceable >= 3000) return true;

            string path = above.Code?.Path ?? "";
            if (path.IndexOf("slab", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            return false;
        }
    }
}
