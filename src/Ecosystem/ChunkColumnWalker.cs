using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Shared rain-heightmap column descent for chunk ecology scans.</summary>
    internal static class ChunkColumnWalker
    {
        public static int GetColumnTopY(IMapChunk mapChunk, int lx, int lz, int chunkSize, int mapTopY)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int extra = cfg.FoliageColumnScanHeightAboveSurface;
            if (extra <= 0)
            {
                return mapTopY;
            }

            if (mapChunk == null) return mapTopY;

            ushort[] heightmap = mapChunk.RainHeightMap;
            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return mapTopY;

            int surfaceY = heightmap[lz * chunkSize + lx];
            int fromSurface = surfaceY + extra;
            return fromSurface > mapTopY ? mapTopY : fromSurface;
        }

        public static bool ContinueColumnScan(Block block)
        {
            if (block == null || block.Id == 0) return true;
            if (CanopyFoliageRules.IsSeasonalFoliageBlock(block)) return true;

            string path = block.Code?.Path;
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("log-grown-")) return true;
                if (path.StartsWith("leavesbranchy-") || path.StartsWith("leaves-")) return true;
                if (path.StartsWith("wildbeehive-inlog-")) return true;
            }

            return PlantVacancyRules.IsPassThroughForColumnScan(block);
        }
    }
}
