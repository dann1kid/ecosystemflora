using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Live column scan for wild flora parents (flowers, tallgrass, ferns, …).</summary>
    internal static class FloraColumnDiscovery
    {
        public delegate bool FloraFoundHandler(BlockPos pos, Block block, bool needsEstablishment);

        public readonly struct ScanResult
        {
            public readonly int FloraFound;
            public readonly int ColumnsScanned;
            public readonly int ResumeLx;
            public readonly int ResumeLz;
            public readonly bool Completed;

            public ScanResult(int floraFound, int columnsScanned, int resumeLx, int resumeLz, bool completed)
            {
                FloraFound = floraFound;
                ColumnsScanned = columnsScanned;
                ResumeLx = resumeLx;
                ResumeLz = resumeLz;
                Completed = completed;
            }
        }

        public static ScanResult ScanChunkColumns(
            ICoreAPI api,
            IBlockAccessor acc,
            Vec2i chunkCoord,
            FloraFoundHandler onFound,
            int maxHits,
            int maxColumns,
            int startLx = 0,
            int startLz = 0)
        {
            if (api == null || acc == null || onFound == null || maxHits <= 0 || maxColumns <= 0)
            {
                return new ScanResult(0, 0, startLx, startLz, completed: true);
            }

            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            IMapChunk mapChunk = acc.GetMapChunk(chunkCoord.X, chunkCoord.Y);
            int columnTop = acc.MapSizeY - 1;
            ushort[] heightmap = mapChunk?.RainHeightMap;
            var view = new LiveRegistrationColumnView(acc);
            int found = 0;
            int columnsScanned = 0;

            for (int lx = startLx; lx < chunkSize; lx++)
            {
                int lzStart = lx == startLx ? startLz : 0;
                for (int lz = lzStart; lz < chunkSize; lz++)
                {
                    columnsScanned++;

                    int x = x0 + lx;
                    int z = z0 + lz;
                    int topY = ChunkColumnWalker.GetFloraRegistrationScanTopY(heightmap, lx, lz, chunkSize, columnTop);

                    if (RegistrationColumnFlowerScan.TryFindTopReproducer(
                            view,
                            api,
                            x,
                            z,
                            topY,
                            out Block block,
                            out BlockPos pos,
                            out bool needsEstablishment)
                        && onFound(pos, block, needsEstablishment))
                    {
                        found++;
                    }

                    if (found >= maxHits)
                    {
                        return AdvanceCursor(chunkSize, lx, lz, found, columnsScanned);
                    }

                    if (columnsScanned >= maxColumns)
                    {
                        return AdvanceCursor(chunkSize, lx, lz, found, columnsScanned);
                    }
                }
            }

            return new ScanResult(found, columnsScanned, 0, 0, completed: true);
        }

        public static ChunkEcologyColumnPass.Result SupplementEmptyWorkerFlora(
            ICoreAPI api,
            Vec2i chunkCoord,
            in ChunkEcologyColumnPass.Result workerPass,
            int maxHits)
        {
            if (api?.World?.BlockAccessor == null) return workerPass;
            if (workerPass.FlowerHits != null && workerPass.FlowerHits.Count > 0) return workerPass;
            if (workerPass.EstablishingTallgrassHits != null && workerPass.EstablishingTallgrassHits.Count > 0)
            {
                return workerPass;
            }

            var flowerHits = new List<ChunkFlowerHit>();
            var establishingHits = new List<ChunkFlowerHit>();
            IBlockAccessor acc = api.World.BlockAccessor;

            ScanChunkColumns(
                api,
                acc,
                chunkCoord,
                (pos, block, needsEstablishment) =>
                {
                    if (needsEstablishment)
                    {
                        establishingHits.Add(new ChunkFlowerHit(pos, block.Code));
                    }
                    else
                    {
                        flowerHits.Add(new ChunkFlowerHit(pos, block.Code));
                    }

                    return flowerHits.Count + establishingHits.Count < maxHits;
                },
                maxHits,
                maxColumns: GlobalConstants.ChunkSize * GlobalConstants.ChunkSize);

            return new ChunkEcologyColumnPass.Result(
                flowerHits,
                workerPass.VineHits,
                workerPass.TreeHits,
                establishingHits,
                workerPass.FoliageIndexed,
                workerPass.FoliageChanged,
                workerPass.ResumeLx,
                workerPass.ResumeLz,
                workerPass.ResumeY,
                workerPass.Completed);
        }

        static ScanResult AdvanceCursor(int chunkSize, int lx, int lz, int found, int columnsScanned)
        {
            lz++;
            if (lz >= chunkSize)
            {
                lx++;
                lz = 0;
            }

            bool completed = lx >= chunkSize;
            return new ScanResult(found, columnsScanned, completed ? 0 : lx, completed ? 0 : lz, completed);
        }
    }
}
