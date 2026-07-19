using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationChunkSnapshotTests
    {
        [Fact]
        public void CellIndex_RoundTrips()
        {
            int yStride = 128;
            int idx = RegistrationChunkSnapshot.CellIndex(5, 7, 42, yStride);
            Assert.Equal((5 * 32 + 7) * yStride + 42, idx);
        }

        [Fact]
        public void GetBlockId_OutOfRange_ReturnsZero()
        {
            var snap = new RegistrationChunkSnapshot(
                new Vec2i(0, 0),
                mapSizeY: 256,
                yStride: 4,
                rainHeightMap: new ushort[32 * 32],
                blockIds: new int[32 * 32 * 4]);

            Assert.Equal(0, snap.GetBlockId(-1, 0, 0));
            Assert.Equal(0, snap.GetBlockId(0, 0, 99));
            Assert.Equal(0, snap.GetBlockId(0, 0, 4));
        }

        [Fact]
        public void ComputeYStride_UsesFloraScanTop_NotFullMap()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 48 };

                var rain = new ushort[32 * 32];
                const int surface = 70;
                for (int i = 0; i < rain.Length; i++) rain[i] = surface;

                // Walker index: lz * cs + lx
                int maxTop = RegistrationChunkSnapshotBuilder.ComputeYStride(rain, 32, mapTopY: 255);
                // surface+48 = 118, flora also ensures surface+28 — so 118
                Assert.Equal(118, maxTop);
                Assert.True(maxTop < 255);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void Advance_AllocatesCompactYStride_AndCopiesSurfaceBand()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 48 };

                Block[] blocks = EcologyTestBlocks.CreateCatalog();
                var acc = new EcologyTestBlockAccessor(blocks) { MapSizeY = 256 };
                EcologyTestMapChunk map = acc.GetOrCreateMapChunk(0, 0);

                const int surface = 70;
                for (int lx = 0; lx < 32; lx++)
                {
                    for (int lz = 0; lz < 32; lz++)
                    {
                        map.RainHeightMap[lz * 32 + lx] = surface;
                        acc.SetBlock(blocks[1].BlockId, new BlockPos(lx, surface, lz)); // soil
                        acc.SetBlock(blocks[2].BlockId, new BlockPos(lx, surface + 1, lz)); // flower
                    }
                }

                var builder = new RegistrationChunkSnapshotBuilder(new Vec2i(0, 0));
                bool done = builder.Advance(acc, map, maxCells: 0, deadlineTicks: 0);

                Assert.True(done);
                Assert.True(builder.Completed);
                Assert.NotNull(builder.Snapshot);
                Assert.Equal(256, builder.Snapshot.MapSizeY);
                Assert.Equal(119, builder.Snapshot.YStride); // maxTop 118 + 1
                Assert.Equal(32 * 32 * 119, builder.Snapshot.BlockIds.Length);
                Assert.True(builder.Snapshot.BlockIds.Length < 32 * 32 * 256);

                Assert.Equal(blocks[2].Id, builder.Snapshot.GetBlockId(0, 0, surface + 1));
                Assert.Equal(0, builder.Snapshot.GetBlockId(0, 0, 200)); // above stride
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void Advance_ResumesMidColumn_WithoutRestartingY()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 48 };

                Block[] blocks = EcologyTestBlocks.CreateCatalog();
                var acc = new EcologyTestBlockAccessor(blocks) { MapSizeY = 128 };
                EcologyTestMapChunk map = acc.GetOrCreateMapChunk(0, 0);
                for (int i = 0; i < map.RainHeightMap.Length; i++) map.RainHeightMap[i] = 40;

                // Mark a unique block high in the first column so resume must reach it.
                acc.SetBlock(blocks[2].BlockId, new BlockPos(0, 40, 0));

                var builder = new RegistrationChunkSnapshotBuilder(new Vec2i(0, 0));
                Assert.False(builder.Advance(acc, map, maxCells: 20, deadlineTicks: 0));
                Assert.False(builder.Completed);

                // Finish with more budget.
                Assert.True(builder.Advance(acc, map, maxCells: 0, deadlineTicks: 0));
                Assert.Equal(blocks[2].Id, builder.Snapshot.GetBlockId(0, 0, 40));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }

    public class RegistrationBurstAndBackpressureTests
    {
        [Fact]
        public void BurstAndPriorityBudgets_DefaultToPacedSlice()
        {
            var cfg = new EcosystemConfig();
            Assert.Equal(8, cfg.BurstRegistrationBudgetMs);
            Assert.Equal(8, cfg.PriorityRegistrationBudgetMs);
            Assert.Equal(6, cfg.ResolveMaxRegistrationSolvePending());
            Assert.Equal(3, cfg.ResolveMaxActiveRegistrationSnapshots());
            Assert.Equal(3, cfg.ResolveMaxRegistrationSolveDrainPerTick());
        }

        [Fact]
        public void RegistrationScanner_RejectsWhenPendingCapReached()
        {
            using var scanner = new BackgroundRegistrationScanner();
            scanner.ConfigureLimits(pendingCap: 2, completedCap: 2);

            Block[] blocks = EcologyTestBlocks.CreateCatalog();
            var rain = new ushort[32 * 32];
            var ids = new int[32 * 32 * 4];
            var request = new ChunkEcologyColumnPass.Request
            {
                MaxFlowerHits = 1,
                MaxTreeHits = 1,
                MaxVineHits = 0,
            };

            Assert.True(scanner.TrySubmit(
                new RegistrationChunkSnapshot(new Vec2i(0, 0), 4, 4, rain, ids),
                in request,
                highPriority: false,
                out _));
            Assert.True(scanner.TrySubmit(
                new RegistrationChunkSnapshot(new Vec2i(1, 0), 4, 4, rain, ids),
                in request,
                highPriority: false,
                out _));
            Assert.False(scanner.TrySubmit(
                new RegistrationChunkSnapshot(new Vec2i(2, 0), 4, 4, rain, ids),
                in request,
                highPriority: false,
                out _));
            Assert.Equal(2, scanner.PendingCount);
            Assert.True(scanner.RejectedSubmitCount >= 1);
        }
    }
}
