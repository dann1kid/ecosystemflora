using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class CellDisplacementIntegrationTests
    {
        static void BuildDisplacementFixture(EcosystemSimHost host)
        {
            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .RainHeight(11, 10, 64)
                .RainHeight(9, 10, 64)
                .RainHeight(10, 9, 64)
                .RainHeight(10, 11, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Column(11, 10, c => c.Soil(63).Flower("cowparsley", 64))
                .Column(9, 10, c => c.Soil(63).Tallgrass("veryshort", 64))
                .Column(10, 9, c => c.Soil(63).Tallgrass("veryshort", 64))
                .Column(10, 11, c => c.Soil(63).Tallgrass("veryshort", 64))
                .Build();
        }

        [Fact]
        public void Catmint_can_displace_cowparsley_by_competition_score()
        {
            var cfg = EcosystemConfig.ForIntegrationTests();
            using var host = EcosystemSimHost.Create(cfg);
            BuildDisplacementFixture(host);
            host.LoadChunk(new Vec2i(0, 0));

            host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry);
            var targetPos = new BlockPos(11, 64, 10);
            CellBlockSnapshot snap = CellBlockSnapshot.Sample(host.Accessor, targetPos);

            bool canDisplace = CellCompetition.CanDisplace(
                host.Api.Object,
                entry.Requirements,
                host.BlockAt(targetPos),
                targetPos,
                cfg.HarshWildPlants,
                in snap,
                out float challengerScore,
                out float incumbentScore);

            Assert.True(canDisplace, $"challenger={challengerScore} incumbent={incumbentScore} margin={cfg.DisplacementHoldMargin}");
        }

        [Fact]
        public void Catmint_displaces_cowparsley_on_occupied_neighbor()
        {
            var cfg = EcosystemConfig.ForIntegrationTests();
            cfg.EnableEmptyFirstSpreadCollect = false;

            using var host = EcosystemSimHost.Create(cfg);
            BuildDisplacementFixture(host);
            host.LoadChunk(new Vec2i(0, 0));
            Assert.Equal(2, host.Eco.Test_RegistryCount);

            host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry);
            entry.NextAttemptHours = 0;
            entry.NextSpawnAllowedAtHours = 0;

            host.Eco.Test_TryGetRegistryEntry(new BlockPos(11, 64, 10), out ReproducerEntry incumbent);
            incumbent.NextAttemptHours = 9999;
            incumbent.NextSpawnAllowedAtHours = 9999;

            host.TickReproduce(times: 4);

            string displacedCode = host.BlockCodeAt(new BlockPos(11, 64, 10));
            Assert.Equal("ecosystemflora:juvenile-flower-catmint-free", displacedCode);
        }
    }
}
