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
            ushort[] heightmap = mapChunk?.RainHeightMap;
            return GetColumnTopY(heightmap, lx, lz, chunkSize, mapTopY);
        }

        public static int GetColumnTopY(ushort[] heightmap, int lx, int lz, int chunkSize, int mapTopY)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int extra = cfg.FoliageColumnScanHeightAboveSurface;
            if (extra <= 0)
            {
                return mapTopY;
            }

            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return mapTopY;

            int surfaceY = heightmap[lz * chunkSize + lx];
            int fromSurface = surfaceY + extra;
            return fromSurface > mapTopY ? mapTopY : fromSurface;
        }

        /// <summary>Top Y for flora registration scans (matches tree crown depth above rain surface).</summary>
        public static int GetFloraRegistrationScanTopY(ushort[] heightmap, int lx, int lz, int chunkSize, int mapTopY)
        {
            int topY = GetColumnTopY(heightmap, lx, lz, chunkSize, mapTopY);
            if (heightmap == null || heightmap.Length < chunkSize * chunkSize) return topY;

            int surfaceY = heightmap[lz * chunkSize + lx];
            if (surfaceY <= 0) return topY;

            int fromSurface = surfaceY + 28;
            if (fromSurface > topY) topY = fromSurface;
            return topY > mapTopY ? mapTopY : topY;
        }

        public static bool ContinueColumnScan(Block block)
        {
            if (block == null || block.Id == 0) return true;
            if (CanopyFoliageRules.IsSeasonalFoliageBlock(block)) return true;

            string path = block.Code?.Path;
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("log-grown-")) return true;
                if (path.StartsWith("ferntree-normal-")) return true;
                if (path.StartsWith("leavesbranchy-") || path.StartsWith("leaves-")) return true;
                if (path.StartsWith("wildbeehive-inlog-")) return true;
            }

            return PlantVacancyRules.IsPassThroughForColumnScan(block);
        }
    }
}
