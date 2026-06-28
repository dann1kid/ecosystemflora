using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class RegistrationScanIntegrationTests
    {
        [Fact]
        public void ChunkScan_registers_meadow_flower_in_reproducer_registry()
        {
            using var host = EcosystemSimHost.Create();
            host.World
                .Chunk(0, 0)
                .RainHeight(10, 10, 64)
                .Column(10, 10, c => c.Soil(63).Flower("catmint", 64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));

            Assert.Equal(1, host.Eco.Test_RegistryCount);
            Assert.True(host.Eco.Test_TryGetRegistryEntry(new BlockPos(10, 64, 10), out ReproducerEntry entry));
            Assert.Equal("catmint", entry.Requirements.Species);
        }

        [Fact]
        public void ChunkScan_queues_tallgrass_establishment_instead_of_registry()
        {
            using var host = EcosystemSimHost.Create();
            host.World
                .Chunk(0, 0)
                .RainHeight(8, 8, 64)
                .Column(8, 8, c => c.Soil(63).Tallgrass("veryshort", 64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));

            Assert.Equal(0, host.Eco.Test_RegistryCount);
        }
    }
}
