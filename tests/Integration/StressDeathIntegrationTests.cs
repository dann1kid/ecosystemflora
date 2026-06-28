using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class StressDeathIntegrationTests
    {
        static EcosystemConfig StressConfig()
        {
            var cfg = EcosystemConfig.ForIntegrationTests();
            cfg.EnableStressDeath = true;
            cfg.HarshWildPlants = true;
            cfg.UseNicheContext = false;
            cfg.MaxFailedSurvivalChecks = 2;
            cfg.StressRecheckHours = 1;
            return cfg;
        }

        [Fact]
        public void Catmint_dies_after_repeated_climate_stress_checks()
        {
            EcosystemConfig cfg = StressConfig();
            using var host = EcosystemSimHost.Create(cfg);
            var plantPos = new BlockPos(10, 64, 10);
            host.Accessor.Temperature = -10f;

            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));
            Assert.True(host.Eco.Test_TryGetRegistryEntry(plantPos, out ReproducerEntry entry));

            for (int i = 0; i < 3; i++)
            {
                entry.NextStressCheckAt = 0;
                host.TickStress();
                host.AdvanceHours(cfg.StressRecheckHours);
            }

            Assert.Equal("game:air", host.BlockCodeAt(plantPos));
            Assert.Equal(0, host.Eco.Test_RegistryCount);
        }
    }
}
