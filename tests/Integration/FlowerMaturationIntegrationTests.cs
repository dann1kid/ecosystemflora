using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class FlowerMaturationIntegrationTests
    {
        [Fact]
        public void Spread_juvenile_catmint_matures_after_calendar_hours()
        {
            using var host = EcosystemSimHost.Create();
            var spreadPos = new BlockPos(11, 64, 10);
            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .RainHeight(11, 10, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Column(11, 10, c => c.Soil(63).Air(64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));
            host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry);
            entry.NextAttemptHours = 0;
            entry.NextSpawnAllowedAtHours = 0;

            host.TickReproduce(times: 8);
            Assert.Equal("ecosystemflora:juvenile-flower-catmint-free", host.BlockCodeAt(spreadPos));

            host.AdvanceHours(49);
            host.TickReproduce();

            Assert.Equal("game:flower-catmint-free", host.BlockCodeAt(spreadPos));
            Assert.True(host.Eco.Test_TryGetRegistryEntry(spreadPos, out ReproducerEntry matureEntry));
            Assert.Equal("catmint", matureEntry.Requirements.Species);
        }
    }
}
