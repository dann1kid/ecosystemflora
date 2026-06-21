using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SpreadSolveMatTests
    {
        [Fact]
        public void CanBackgroundSolve_MatSpread_ReturnsTrue()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = true };
                var req = new PlantRequirements
                {
                    Habitat = EcologyHabitat.ReedNearWater,
                    Species = "coopersreed",
                };
                RhizomeSpread.ApplyTo(req);

                Assert.True(SpreadSolveBatchBuilder.CanBackgroundSolve(req));
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void PickWinners_MatCell_UsesMatPreflight()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                EcosystemConfig.Loaded = new EcosystemConfig { UseRhizomeSpreadForReeds = true };
                var req = new PlantRequirements
                {
                    Habitat = EcologyHabitat.ReedNearWater,
                    Species = "coopersreed",
                    MinRain = 0f,
                    MaxRain = 1f,
                };
                RhizomeSpread.ApplyTo(req);

                var cell = new SpreadSolveCell(
                    new BlockPos(10, 64, 10),
                    0, 1, 9999, 200, SoilKind.MediumFert, true, true, false,
                    hasShallowWater: true, true, 0.5f, 0.2f, FloraContext.Open,
                    (int)MoistureLevel.Mesic, (int)LightLevel.Partial, 1f, true, matVacancyOk: true);

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
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}
