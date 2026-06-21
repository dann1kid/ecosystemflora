using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadSolverTests
    {
        static PlantRequirements DefaultReq() => new PlantRequirements
        {
            Species = "catmint",
            Habitat = EcologyHabitat.Terrestrial,
            MinTemp = -5f,
            MaxTemp = 50f,
            MinRain = 0f,
            MaxRain = 1f,
            MinForest = 0f,
            MaxForest = 1f,
        };

        static SpreadSolveCell GoodEmptyCell(int x, int y, int z) => new SpreadSolveCell(
            new BlockPos(x, y, z),
            spaceBlockId: 0,
            groundBlockId: 1,
            spaceReplaceable: 9999,
            groundFertility: 200,
            groundSoilKinds: SoilKind.MediumFert,
            groundSideSolid: true,
            isEmpty: true,
            touchesFluid: false,
            hasShallowWater: false,
            hasClimate: true,
            worldgenRainfall: 0.5f,
            localForestCover: 0.3f,
            floraContext: FloraContext.Open,
            nicheMoisture: (int)MoistureLevel.Mesic,
            nicheLight: (int)LightLevel.Partial,
            myceliumFitnessMult: 1f,
            spacingOk: true);

        [Fact]
        public void PickWinners_EmptyCellAboveMinFitness_ReturnsWinner()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig();
                var cells = new List<SpreadSolveCell> { GoodEmptyCell(10, 64, 10) };
                var blocks = new List<Vintagestory.API.Common.Block> { null, null };
                var winners = new List<SpreadSolveWinner>();
                var rand = new System.Random(42);

                int picked = SpreadSolver.PickWinners(
                    cells,
                    blocks,
                    DefaultReq(),
                    minFitness: 0.01f,
                    harshClimate: true,
                    SpreadCollectPhase.All,
                    seasonSpreadMult: 1f,
                    seedFitnessScale: 1f,
                    maxSpawns: 1,
                    rand,
                    winners);

                Assert.Equal(1, picked);
                Assert.Single(winners);
                Assert.Equal(new BlockPos(10, 64, 10), winners[0].TargetPos);
                Assert.False(winners[0].Displacing);
                Assert.True(winners[0].Fitness > 0f);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void PickWinners_TouchesFluid_SkipsCell()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig();
                SpreadSolveCell wet = GoodEmptyCell(10, 64, 10);
                wet = new SpreadSolveCell(
                    new BlockPos(10, 64, 10),
                    0, 1, 9999, 200, SoilKind.MediumFert, true, true, touchesFluid: true,
                    false, true, 0.5f, 0.3f, FloraContext.Open,
                    (int)MoistureLevel.Mesic, (int)LightLevel.Partial, 1f, true);

                var winners = new List<SpreadSolveWinner>();
                int picked = SpreadSolver.PickWinners(
                    new List<SpreadSolveCell> { wet },
                    new List<Vintagestory.API.Common.Block> { null, null },
                    DefaultReq(),
                    0.01f,
                    true,
                    SpreadCollectPhase.All,
                    1f,
                    1f,
                    1,
                    new System.Random(1),
                    winners);

                Assert.Equal(0, picked);
                Assert.Empty(winners);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void PickWinners_EmptyFirstTwoPhase_PrefersEmptyCell()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableEmptyFirstSpreadCollect = true,
                    UseCellDisplacement = true,
                };

                var cells = new List<SpreadSolveCell>
                {
                    GoodEmptyCell(10, 64, 10),
                    new SpreadSolveCell(
                        new BlockPos(12, 64, 12),
                        spaceBlockId: 0,
                        groundBlockId: 1,
                        spaceReplaceable: 9999,
                        groundFertility: 200,
                        groundSoilKinds: SoilKind.MediumFert,
                        groundSideSolid: true,
                        isEmpty: false,
                        touchesFluid: false,
                        hasShallowWater: false,
                        hasClimate: true,
                        worldgenRainfall: 0.5f,
                        localForestCover: 0.3f,
                        floraContext: FloraContext.Open,
                        nicheMoisture: (int)MoistureLevel.Mesic,
                        nicheLight: (int)LightLevel.Partial,
                        myceliumFitnessMult: 1f,
                        spacingOk: true),
                };

                var winners = new List<SpreadSolveWinner>();
                int picked = SpreadSolver.PickWinners(
                    cells,
                    new List<Vintagestory.API.Common.Block> { null, null },
                    DefaultReq(),
                    0.01f,
                    true,
                    SpreadCollectPhase.All,
                    1f,
                    1f,
                    1,
                    new System.Random(7),
                    winners,
                    emptyFirstTwoPhase: true);

                Assert.Equal(1, picked);
                Assert.Equal(new BlockPos(10, 64, 10), winners[0].TargetPos);
                Assert.False(winners[0].Displacing);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void PickWinners_EmptyFirstTwoPhase_NoEmptyCandidates_ClearsBeforeDisplacementPass()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig
                {
                    EnableEmptyFirstSpreadCollect = true,
                    UseCellDisplacement = true,
                };

                var winners = new List<SpreadSolveWinner>();
                int picked = SpreadSolver.PickWinners(
                    new List<SpreadSolveCell>
                    {
                        new SpreadSolveCell(
                            new BlockPos(12, 64, 12),
                            spaceBlockId: 0,
                            groundBlockId: 1,
                            spaceReplaceable: 9999,
                            groundFertility: 200,
                            groundSoilKinds: SoilKind.MediumFert,
                            groundSideSolid: true,
                            isEmpty: false,
                            touchesFluid: false,
                            hasShallowWater: false,
                            hasClimate: true,
                            worldgenRainfall: 0.5f,
                            localForestCover: 0.3f,
                            floraContext: FloraContext.Open,
                            nicheMoisture: (int)MoistureLevel.Mesic,
                            nicheLight: (int)LightLevel.Partial,
                            myceliumFitnessMult: 1f,
                            spacingOk: true),
                    },
                    new List<Vintagestory.API.Common.Block> { null, null },
                    DefaultReq(),
                    0.01f,
                    true,
                    SpreadCollectPhase.All,
                    1f,
                    1f,
                    1,
                    new System.Random(3),
                    winners,
                    emptyFirstTwoPhase: true);

                Assert.Equal(0, picked);
                Assert.Empty(winners);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}
