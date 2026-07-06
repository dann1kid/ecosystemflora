using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Keeps <c>-free</c> / <c>-snow</c> cover variants aligned with climate and snow layer.
    /// Vanilla frostable blocks self-update; mod-placed phase blocks need explicit sync.
    /// </summary>
    internal static class PlantSnowCoverSync
    {
        public static bool TrySyncCover(ICoreAPI api, BlockPos pos, Block current = null)
        {
            if (api?.World?.BlockAccessor == null || pos == null) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, pos)) return false;

            current ??= api.World.BlockAccessor.GetBlock(pos);
            if (current?.Code == null || current.Id == 0) return false;
            if (!PlantSnowCover.BlockHasCoverVariant(current.Code)) return false;

            bool wantSnow = PlantSnowCover.ResolveWantsSnowCover(api, pos);
            bool hasSnow = PlantSnowCover.PathHasSnowCover(current.Code.Path);
            if (wantSnow == hasSnow) return false;

            AssetLocation targetCode = PlantSnowCover.CodeWithCover(current.Code, wantSnow);
            Block target = api.World.GetBlock(targetCode);
            if (target == null || target.Id == 0 || target.Id == current.Id) return false;

            api.World.BlockAccessor.SetBlock(target.Id, pos);
            api.World.BlockAccessor.MarkBlockDirty(pos);
            return true;
        }
    }
}
