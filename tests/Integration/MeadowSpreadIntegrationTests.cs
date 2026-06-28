using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class MeadowSpreadIntegrationTests
    {
        [Fact]
        public void Catmint_spreads_juvenile_to_prepared_neighbor()
        {
            using var host = EcosystemSimHost.Create();
            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .RainHeight(11, 10, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Column(11, 10, c => c.Soil(63).Air(64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));
            Assert.Equal(1, host.Eco.Test_RegistryCount);

            host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry);
            entry.NextAttemptHours = 0;
            entry.NextSpawnAllowedAtHours = 0;

            host.TickReproduce(times: 8);

            string neighborCode = host.BlockCodeAt(new BlockPos(11, 64, 10));
            Assert.Equal("ecosystemflora:juvenile-flower-catmint-free", neighborCode);
        }

        [Fact]
        public void Catmint_spread_respects_post_spread_cooldown()
        {
            using var host = EcosystemSimHost.Create();
            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .RainHeight(11, 10, 64)
                .RainHeight(12, 10, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Column(11, 10, c => c.Soil(63).Air(64))
                .Column(12, 10, c => c.Soil(63).Air(64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));
            host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry);
            entry.NextAttemptHours = 0;
            entry.NextSpawnAllowedAtHours = 0;

            host.TickReproduce(times: 8);
            Assert.Equal("ecosystemflora:juvenile-flower-catmint-free", host.BlockCodeAt(new BlockPos(11, 64, 10)));

            double cooldownEnd = entry.NextSpawnAllowedAtHours;
            Assert.True(cooldownEnd > host.Calendar.TotalHours);

            entry.NextAttemptHours = 0;
            host.TickReproduce(times: 8);
            Assert.Equal("game:air", host.BlockCodeAt(new BlockPos(12, 64, 10)));
        }
    }
}
