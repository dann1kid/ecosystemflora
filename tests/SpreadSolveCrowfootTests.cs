using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadSolveCrowfootTests
    {
        [Fact]
        public void CanBackgroundSolve_Watercrowfoot_ReturnsTrue()
        {
            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.UnderwaterColumn,
                Species = "watercrowfoot",
                SpreadMode = SpreadMode.Independent,
                MinWaterDepth = 2,
                MaxWaterDepth = 8,
            };

            Assert.True(SpreadSolveBatchBuilder.CanBackgroundSolve(req));
        }

        [Fact]
        public void PickWinners_CrowfootCell_UsesCrowfootPreflight()
        {
            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.UnderwaterColumn,
                Species = "watercrowfoot",
                SpreadMode = SpreadMode.Independent,
                MinWaterDepth = 2,
                MaxWaterDepth = 8,
                MinRain = 0f,
                MaxRain = 1f,
            };

            var cell = new SpreadSolveCell(
                new BlockPos(10, 62, 10),
                0, 1, 9999, 200, SoilKind.MediumFert, true, true, true,
                hasShallowWater: false, true, 0.5f, 0.2f, FloraContext.Open,
                (int)MoistureLevel.Mesic, (int)LightLevel.Partial, 1f, true,
                matVacancyOk: true,
                waterColumnDepth: 4);

            var winners = new List<SpreadSolveWinner>();
            int picked = SpreadSolver.PickWinners(
                new List<SpreadSolveCell> { cell },
                new List<Vintagestory.API.Common.Block> { null, null },
                req,
                0.01f,
                true,
                SpreadCollectPhase.All,
                1f,
                1f,
                1,
                new System.Random(1),
                winners);

            Assert.Equal(1, picked);
            Assert.False(winners[0].Displacing);
        }

        [Fact]
        public void PickWinners_CrowfootCell_RejectsShallowWater()
        {
            var req = new PlantRequirements
            {
                Habitat = EcologyHabitat.UnderwaterColumn,
                Species = "watercrowfoot",
                SpreadMode = SpreadMode.Independent,
                MinWaterDepth = 2,
                MaxWaterDepth = 8,
            };

            var cell = new SpreadSolveCell(
                new BlockPos(10, 62, 10),
                0, 1, 9999, 200, SoilKind.MediumFert, true, true, true,
                hasShallowWater: false, true, 0.5f, 0.2f, FloraContext.Open,
                (int)MoistureLevel.Mesic, (int)LightLevel.Partial, 1f, true,
                matVacancyOk: true,
                waterColumnDepth: 1);

            var winners = new List<SpreadSolveWinner>();
            int picked = SpreadSolver.PickWinners(
                new List<SpreadSolveCell> { cell },
                new List<Vintagestory.API.Common.Block> { null, null },
                req,
                0.01f,
                true,
                SpreadCollectPhase.All,
                1f,
                1f,
                1,
                new System.Random(1),
                winners);

            Assert.Equal(0, picked);
        }
    }
}
