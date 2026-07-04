using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class CrowfootSpreadGuardTests
    {
        static Block Block(string code, int id = 1, bool sideSolidUp = false)
        {
            var block = new Block { BlockId = id, Code = new AssetLocation(code) };
            if (sideSolidUp) block.SideSolid[BlockFacing.UP.Index] = true;
            return block;
        }

        [Fact]
        public void CrowfootMatSpread_IsFrontier_SurroundedByCrowfoot_NotFrontier()
        {
            Block air = Block("game:air", 0);
            Block section = Block("game:aquatic-watercrowfoot-section", 1);
            Block water = Block("game:water-still-7", 2);
            water.LiquidLevel = 7;
            Block gravel = Block("game:gravel-granite", 3, sideSolidUp: true);
            gravel.Fertility = 100;

            var acc = new EcologyTestBlockAccessor(new[] { air, section, water, gravel });
            acc.SetBlock(3, new BlockPos(10, 61, 10));
            acc.SetBlock(2, new BlockPos(10, 62, 10));
            acc.SetBlock(1, new BlockPos(10, 62, 10));
            acc.SetBlock(2, new BlockPos(11, 62, 10));
            acc.SetBlock(1, new BlockPos(11, 62, 10));
            acc.SetBlock(2, new BlockPos(9, 62, 10));
            acc.SetBlock(1, new BlockPos(9, 62, 10));
            acc.SetBlock(2, new BlockPos(10, 62, 11));
            acc.SetBlock(1, new BlockPos(10, 62, 11));
            acc.SetBlock(2, new BlockPos(10, 62, 9));
            acc.SetBlock(1, new BlockPos(10, 62, 9));

            bool frontier = CrowfootMatSpread.IsFrontier(
                acc,
                new BlockPos(10, 62, 10),
                "watercrowfoot",
                verticalReach: 2);

            Assert.False(frontier);
        }

        [Fact]
        public void IsPlantableWaterCell_RejectsSolidNonWaterBlock()
        {
            Block air = Block("game:air", 0);
            Block trap = Block("primitivesurvival:fishtrap-east", 1);
            trap.SideSolid[BlockFacing.UP.Index] = true;
            var acc = new EcologyTestBlockAccessor(new[] { air, trap });
            acc.SetBlock(1, new BlockPos(10, 62, 10));

            Assert.False(CrowfootSpreadGuard.IsPlantableWaterCell(acc, new BlockPos(10, 62, 10)));
        }

        [Fact]
        public void IsPlantableWaterCell_AcceptsDedicatedWaterBlock()
        {
            Block air = Block("game:air", 0);
            Block water = Block("game:water-still-7", 1);
            water.LiquidLevel = 7;
            var acc = new EcologyTestBlockAccessor(new[] { air, water });
            acc.SetBlock(1, new BlockPos(10, 62, 10));

            Assert.True(CrowfootSpreadGuard.IsPlantableWaterCell(acc, new BlockPos(10, 62, 10)));
        }

        [Fact]
        public void PassesSpreadTargetGate_RejectsOccupiedCrowfootSection()
        {
            Block air = Block("game:air", 0);
            Block section = Block("game:aquatic-watercrowfoot-section", 1);
            Block gravel = Block("game:gravel-granite", 2, sideSolidUp: true);
            gravel.Fertility = 100;

            var acc = new EcologyTestBlockAccessor(new[] { air, section, gravel });
            acc.SetBlock(2, new BlockPos(10, 61, 10));
            acc.SetBlock(1, new BlockPos(10, 62, 10));

            var req = new PlantRequirements
            {
                Species = "watercrowfoot",
                Habitat = EcologyHabitat.UnderwaterColumn,
                MinWaterDepth = 2,
                MaxWaterDepth = 6,
            };

            bool ok = SpreadPreflight.PassesSpreadTargetGate(
                acc,
                new BlockPos(10, 62, 10),
                req,
                displacing: false,
                out _);

            Assert.False(ok);
        }

        [Fact]
        public void EffectiveSeedDispersalChance_WatercrowfootZeroInCsv_ReturnsZero()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { RhizomeSeedDispersalChanceScale = 1f };
                var req = new PlantRequirements
                {
                    Species = "watercrowfoot",
                    Habitat = EcologyHabitat.UnderwaterColumn,
                    SpreadMode = SpreadMode.RhizomeMat,
                    SeedDispersalChance = 0f,
                    SeedDispersalRadius = 0,
                };

                float chance = RhizomeSpread.EffectiveSeedDispersalChance(req);
                Assert.Equal(0f, chance);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void CanBackgroundSolve_WatercrowfootRhizomeMat_ReturnsTrue()
        {
            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.UnderwaterColumn,
                Species = "watercrowfoot",
                SpreadMode = SpreadMode.RhizomeMat,
            };

            Assert.True(SpreadSolveBatchBuilder.CanBackgroundSolve(req));
            Assert.True(SpreadSolveBatchBuilder.UsesCrowfootSpread(req));
            Assert.False(SpreadSolveBatchBuilder.UsesMatSpread(req));
        }
    }
}
