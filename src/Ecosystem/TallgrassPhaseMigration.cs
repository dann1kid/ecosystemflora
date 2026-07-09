using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Reverts legacy <c>tallgrassphase-*</c> blocks to vanilla <c>game:tallgrass-*</c> so frostable
    /// cover is handled entirely by the game engine.
    /// </summary>
    internal static class TallgrassPhaseMigration
    {
        public static bool TryMigrateLegacyPhaseBlock(ICoreAPI api, BlockPos pos, Block current = null)
        {
            if (api?.World?.BlockAccessor == null || pos == null) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            current ??= api.World.BlockAccessor.GetBlock(pos);
            if (!TallgrassPhenologyBlocks.IsPhaseBlock(current)) return false;

            AssetLocation vanillaCode = new AssetLocation("game:tallgrass-veryshort-free");
            Block target = api.World.GetBlock(vanillaCode);
            if (target == null || target.Id == 0 || target.Id == current.Id) return false;

            api.World.BlockAccessor.SetBlock(target.Id, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
            return true;
        }
    }
}
