using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Remaps legacy bare fern phase block codes (pre cover-variant migration)
    /// to <c>-free</c> cover variants.
    /// </summary>
    internal static class LegacyPhaseBlockMigration
    {
        internal const int RemapDelayMs = 250;

        public static void ScheduleRemapColumn(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;

            Vec2i coord = chunkCoord.Copy();
            api.Event.RegisterCallback(_ => RemapColumnAt(api, coord), RemapDelayMs);
        }

        static void RemapColumnAt(ICoreAPI api, Vec2i chunkCoord)
        {
            IBlockAccessor acc = api.World?.BlockAccessor;
            if (acc == null) return;

            int cs = GlobalConstants.ChunkSize;
            int baseX = chunkCoord.X * cs;
            int baseZ = chunkCoord.Y * cs;
            int maxY = acc.MapSizeY;

            for (int y = 0; y < maxY; y++)
            {
                for (int lx = 0; lx < cs; lx++)
                {
                    for (int lz = 0; lz < cs; lz++)
                    {
                        var pos = new BlockPos(baseX + lx, y, baseZ + lz);
                        Block block = acc.GetBlock(pos);
                        TryRemapAt(acc, pos, block);
                        block = acc.GetBlock(pos);
                        if (PlantSnowCover.ShouldSyncCoverVariant(block?.Code))
                        {
                            PlantSnowCoverSync.TrySyncCover(api, pos, block);
                        }
                    }
                }
            }
        }

        static void TryRemapAt(IBlockAccessor acc, BlockPos pos, Block block)
        {
            AssetLocation targetCode = ResolveRemapTarget(block?.Code);
            if (targetCode == null) return;

            Block target = acc.GetBlock(targetCode);
            if (target == null || target.Id == 0 || target.Id == block.Id) return;

            acc.SetBlock(target.Id, pos);
        }

        internal static AssetLocation ResolveRemapTarget(AssetLocation code)
        {
            if (code?.Domain != "ecosystemflora" || code.Path == null) return null;
            if (!code.Path.StartsWith("fernphase-")) return null;
            if (!PlantSnowCover.IsLegacyBareFernPhasePath(code.Path)) return null;

            return new AssetLocation(
                code.Domain,
                code.Path + JuvenileBlockNaming.FreeSuffix);
        }
    }
}
