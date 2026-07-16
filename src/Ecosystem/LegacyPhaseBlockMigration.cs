using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Remaps legacy bare fern phase block codes (pre cover-variant migration)
    /// to <c>-free</c> cover variants. Scans only a band around the rain surface —
    /// never the full MapSizeY volume (that hitch froze SSP on every chunk load).
    /// </summary>
    internal static class LegacyPhaseBlockMigration
    {
        internal const int RemapDelayMs = 250;
        const int BandBelowSurface = 2;
        const int BandAboveSurface = 8;

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
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            ushort[] heightmap = mapChunk?.RainHeightMap;
            int mapTop = acc.MapSizeY - 1;
            var pos = new BlockPos(0);

            for (int lx = 0; lx < cs; lx++)
            {
                for (int lz = 0; lz < cs; lz++)
                {
                    int surfaceY = 64;
                    if (heightmap != null && heightmap.Length >= cs * cs)
                    {
                        surfaceY = heightmap[lz * cs + lx];
                    }

                    int yMin = surfaceY - BandBelowSurface;
                    if (yMin < 0) yMin = 0;
                    int yMax = surfaceY + BandAboveSurface;
                    if (yMax > mapTop) yMax = mapTop;

                    for (int y = yMin; y <= yMax; y++)
                    {
                        pos.Set(baseX + lx, y, baseZ + lz);
                        Block block = acc.GetBlock(pos);
                        if (block == null || block.Id == 0) continue;

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
