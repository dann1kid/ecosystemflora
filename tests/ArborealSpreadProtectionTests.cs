using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class ArborealSpreadProtectionTests
    {
        static Block Block(string code, int blockId = 1) =>
            new Block { BlockId = blockId, Code = new AssetLocation(code) };

        [Theory]
        [InlineData("game:log-grown-oak-ud")]
        [InlineData("game:log-grown-moddedwood-north")]
        [InlineData("game:ferntree-normal-trunk")]
        [InlineData("game:sapling-birch-free")]
        [InlineData("game:fruittree-stem-apple")]
        [InlineData("game:fruittree-young-cherry")]
        public void IsArborealHostBlock_recognizes_trunk_blocks(string code)
        {
            Assert.True(PlantCodeHelper.IsArborealHostBlock(Block(code)));
        }

        [Fact]
        public void IsAnyLogGrownTrunkBlock_accepts_unknown_wood_but_not_aged_snag()
        {
            Assert.True(PlantCodeHelper.IsAnyLogGrownTrunkBlock(Block("game:log-grown-moddedwood-ud")));
            Assert.False(PlantCodeHelper.IsAnyLogGrownTrunkBlock(Block("game:log-grown-aged-oak-ud")));
            Assert.True(PlantCodeHelper.IsTreeLogGrownBlock(Block("game:log-grown-moddedwood-ud")));
        }

        [Fact]
        public void CanDisplaceFromSolveCell_rejects_log_grown_trunk()
        {
            var challenger = new PlantRequirements
            {
                Species = "cornflower",
                Habitat = EcologyHabitat.Terrestrial,
            };
            var trunk = Block("game:log-grown-oak-ud");
            SpreadSolveCell cell = default;

            bool canDisplace = CellCompetition.CanDisplaceFromSolveCell(
                challenger,
                trunk,
                new BlockPos(0, 64, 0),
                in cell,
                harshClimate: false,
                seasonSpreadMult: 1f,
                out float challengerScore,
                out _);

            Assert.False(canDisplace);
            Assert.Equal(0f, challengerScore);
        }

        [Fact]
        public void IsEcologySpreadParent_includes_trunk_but_arboreal_guard_blocks_meadow_use()
        {
            var trunk = Block("game:log-grown-birch-ud");
            Assert.True(PlantCodeHelper.IsEcologySpreadParent(trunk));
            Assert.True(PlantCodeHelper.IsArborealHostBlock(trunk));
        }

        [Fact]
        public void SurfacePlacement_rejects_grass_on_trunk_as_ground()
        {
            Block air = new Block { BlockId = 0 };
            Block trunk = Block("game:log-grown-oak-ud", 1);
            trunk.SideSolid[BlockFacing.UP.Index] = true;
            Block soil = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:soil-medium-normal"),
            };
            soil.SideSolid[BlockFacing.UP.Index] = true;

            var acc = new EcologyTestBlockAccessor(new[] { air, trunk, soil });
            acc.SetBlock(2, new BlockPos(10, 63, 10));
            acc.SetBlock(1, new BlockPos(10, 64, 10));

            bool ok = SurfacePlacement.IsValidPlantSite(
                acc,
                new BlockPos(10, 65, 10),
                new PlantRequirements { Species = "tallgrass", Habitat = EcologyHabitat.Terrestrial });

            Assert.False(ok);
        }

        [Fact]
        public void PassesTerrestrialPhysical_rejects_air_above_log_trunk()
        {
            Block air = new Block { BlockId = 0 };
            Block trunk = Block("game:log-grown-oak-ud", 1);
            trunk.SideSolid[BlockFacing.UP.Index] = true;
            Block soil = new Block
            {
                BlockId = 2,
                Code = new AssetLocation("game:soil-medium-normal"),
            };
            soil.SideSolid[BlockFacing.UP.Index] = true;

            var acc = new EcologyTestBlockAccessor(new[] { air, trunk, soil });
            acc.SetBlock(2, new BlockPos(10, 63, 10));
            acc.SetBlock(1, new BlockPos(10, 64, 10));

            CellBlockSnapshot snap = CellBlockSnapshot.Sample(acc, new BlockPos(10, 65, 10));

            bool ok = SpreadPreflight.PassesPhysicalGate(
                acc,
                new BlockPos(10, 65, 10),
                new PlantRequirements { Species = "cornflower", Habitat = EcologyHabitat.Terrestrial },
                in snap,
                out bool isEmpty);

            Assert.False(ok);
        }

        [Fact]
        public void PassesSpreadTargetGate_rejects_tallgrass_on_player_sapling()
        {
            Block air = new Block { BlockId = 0 };
            Block sapling = Block("game:sapling-oak-free", 1);
            Block grass = Block("game:tallgrass-veryshort-free", 2);
            Block soil = new Block
            {
                BlockId = 3,
                Code = new AssetLocation("game:soil-medium-normal"),
            };
            soil.SideSolid[BlockFacing.UP.Index] = true;

            var acc = new EcologyTestBlockAccessor(new[] { air, sapling, grass, soil });
            var pos = new BlockPos(2, 64, 2);
            acc.SetBlock(1, pos);
            acc.SetBlock(3, new BlockPos(2, 63, 2));

            var requirements = new PlantRequirements
            {
                Species = "tallgrass",
                Habitat = EcologyHabitat.Terrestrial,
            };

            bool ok = SpreadPreflight.PassesSpreadTargetGate(
                acc, pos, requirements, displacing: false, out bool isEmpty);

            Assert.False(ok);
            Assert.False(isEmpty);
        }
    }
}
