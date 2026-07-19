using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ChunkEcologyColumnPassTests
    {
        const int ChunkSize = 32;

        [Fact]
        public void ResolveFlowerScanTopY_NeverBelowColumnScanTop()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                // extra <= 0 → column scan uses full map height; flora top must match.
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 0 };

                var heightmap = new ushort[ChunkSize * ChunkSize];
                heightmap[5 * ChunkSize + 7] = 64;

                int topY = ChunkEcologyColumnPass.ResolveFlowerScanTopY(heightmap, 7, 5, ChunkSize, columnTop: 255);

                Assert.Equal(255, topY);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ResolveFlowerScanTopY_FallsBackToColumnTopWhenHeightmapZero()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 0 };

                var heightmap = new ushort[ChunkSize * ChunkSize];

                int topY = ChunkEcologyColumnPass.ResolveFlowerScanTopY(heightmap, 3, 3, ChunkSize, columnTop: 255);

                Assert.Equal(255, topY);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ResolveFlowerScanTopY_UsesSurfacePlusHeadroom_ByDefault()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { FoliageColumnScanHeightAboveSurface = 48 };

                var heightmap = new ushort[ChunkSize * ChunkSize];
                heightmap[5 * ChunkSize + 7] = 64;

                int topY = ChunkEcologyColumnPass.ResolveFlowerScanTopY(heightmap, 7, 5, ChunkSize, columnTop: 255);

                Assert.Equal(112, topY); // 64 + 48
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void ResolveFlowerScanTopY_FallsBackWhenHeightmapMissing()
        {
            int topY = ChunkEcologyColumnPass.ResolveFlowerScanTopY(null, 0, 0, ChunkSize, columnTop: 200);

            Assert.Equal(200, topY);
        }

        [Fact]
        public void TryFindTopReproducer_FindsGrassAboveRainHeightmapBand()
        {
            bool priorMaturation = EcosystemConfig.Loaded.EnableTallgrassSpreadMaturation;
            EcosystemConfig.Loaded.EnableTallgrassSpreadMaturation = false;
            try
            {
                var grassCode = new AssetLocation("game:tallgrass-tall-fern-free");
                var grass = new Block { BlockId = 2, Code = grassCode, Replaceable = 3000 };
                grass.BlockMaterial = EnumBlockMaterial.Plant;

                var soilCode = new AssetLocation("game:soil-medium-normal");
                var soil = new Block { BlockId = 3, Code = soilCode, Replaceable = 100 };
                soil.BlockMaterial = EnumBlockMaterial.Soil;
                soil.SideSolid[BlockFacing.UP.Index] = true;

                var view = new TestColumnView(
                    columnTop: 255,
                    blocks: new Dictionary<(int x, int y, int z), Block>
                    {
                        [(0, 70, 0)] = grass,
                        [(0, 69, 0)] = soil,
                    });

                bool found = RegistrationColumnFlowerScan.TryFindTopReproducer(
                    view,
                    api: null,
                    x: 0,
                    z: 0,
                    scanTopY: 255,
                    out Block block,
                    out BlockPos pos,
                    out bool needsEstablishment);

                Assert.True(found);
                Assert.False(needsEstablishment);
                Assert.Equal(grassCode, block.Code);
                Assert.Equal(70, pos.Y);
            }
            finally
            {
                EcosystemConfig.Loaded.EnableTallgrassSpreadMaturation = priorMaturation;
            }
        }

        [Fact]
        public void TryFindTopReproducer_FindsEaglefernNormalVariant()
        {
            var fernCode = new AssetLocation("game:fern-eaglefern-normal-free");
            var fern = new Block
            {
                BlockId = 5,
                Code = fernCode,
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            var soil = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:soil-medium-normal"),
                Replaceable = 100,
                BlockMaterial = EnumBlockMaterial.Soil,
            };
            soil.SideSolid[BlockFacing.UP.Index] = true;

            var view = new TestColumnView(
                columnTop: 255,
                blocks: new Dictionary<(int x, int y, int z), Block>
                {
                    [(0, 64, 0)] = fern,
                    [(0, 63, 0)] = soil,
                });

            bool found = RegistrationColumnFlowerScan.TryFindTopReproducer(
                view,
                api: null,
                x: 0,
                z: 0,
                scanTopY: 255,
                out Block block,
                out BlockPos pos,
                out bool needsEstablishment);

            Assert.True(found);
            Assert.False(needsEstablishment);
            Assert.Equal(fernCode, block.Code);
            Assert.Equal(64, pos.Y);
            Assert.True(EcosystemParticipant.TryFromBlock(block, out _));
        }

        [Fact]
        public void TryFindTopReproducer_FindsFernUnderTreeTrunk()
        {
            var logCode = new AssetLocation("game:log-grown-oak-ud");
            var log = new Block
            {
                BlockId = 4,
                Code = logCode,
                Replaceable = 100,
                BlockMaterial = EnumBlockMaterial.Wood,
            };

            var fernCode = new AssetLocation("game:fern-eaglefern-normal-free");
            var fern = new Block
            {
                BlockId = 5,
                Code = fernCode,
                Replaceable = 3000,
                BlockMaterial = EnumBlockMaterial.Plant,
            };

            var soil = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:soil-medium-normal"),
                Replaceable = 100,
                BlockMaterial = EnumBlockMaterial.Soil,
            };
            soil.SideSolid[BlockFacing.UP.Index] = true;

            var view = new TestColumnView(
                columnTop: 255,
                blocks: new Dictionary<(int x, int y, int z), Block>
                {
                    [(0, 66, 0)] = log,
                    [(0, 65, 0)] = log,
                    [(0, 64, 0)] = fern,
                    [(0, 63, 0)] = soil,
                });

            bool found = RegistrationColumnFlowerScan.TryFindTopReproducer(
                view,
                api: null,
                x: 0,
                z: 0,
                scanTopY: 255,
                out Block block,
                out BlockPos pos,
                out bool needsEstablishment);

            Assert.True(found);
            Assert.False(needsEstablishment);
            Assert.Equal(fernCode, block.Code);
            Assert.Equal(64, pos.Y);
        }

        sealed class TestColumnView : IRegistrationColumnView
        {
            readonly int columnTop;
            readonly Dictionary<(int x, int y, int z), Block> blocks;

            public TestColumnView(
                int columnTop,
                Dictionary<(int x, int y, int z), Block> blocks)
            {
                this.columnTop = columnTop;
                this.blocks = blocks;
            }

            public bool SupportsFoliageMutation => false;

            public int MapSizeY => columnTop + 1;

            public IMapChunk GetMapChunk(Vec2i chunkCoord) => null;

            public bool IsValidPos(int x, int y, int z) => y >= 0 && y <= columnTop;

            public Block GetBlock(int x, int y, int z)
            {
                if (blocks.TryGetValue((x, y, z), out Block block))
                {
                    return block;
                }

                return new Block { BlockId = 0 };
            }
        }
    }
}
