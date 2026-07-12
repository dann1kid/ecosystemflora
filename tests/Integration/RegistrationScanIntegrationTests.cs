using Vintagestory.API.Common;
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
        public void ChunkScan_registers_eaglefern_in_reproducer_registry()
        {
            using var host = EcosystemSimHost.Create();
            host.World
                .Chunk(0, 0)
                .RainHeight(12, 12, 64)
                .Column(12, 12, c => c.Soil(63).Eaglefern(64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));

            Assert.Equal(1, host.Eco.Test_RegistryCount);
            Assert.True(host.Eco.Test_TryGetRegistryEntry(new BlockPos(12, 64, 12), out ReproducerEntry entry));
            Assert.Equal("eaglefern", entry.Requirements.Species);
        }

        [Fact]
        public void InspectBootstrap_registers_missed_eaglefern()
        {
            using var host = EcosystemSimHost.Create();
            var pos = new BlockPos(12, 64, 12);
            host.World
                .Chunk(0, 0)
                .RainHeight(12, 12, 64)
                .Column(12, 12, c => c.Soil(63).Eaglefern(64))
                .Build();

            Block block = host.BlockAt(pos);
            Assert.True(host.Eco.TryRegisterEligiblePlantAtInspect(pos, block));
            Assert.True(host.Eco.Test_TryGetRegistryEntry(pos, out ReproducerEntry entry));
            Assert.Equal("eaglefern", entry.Requirements.Species);
        }

        [Fact]
        public void ChunkScan_keeps_eaglefern_after_phenology_spread_tick()
        {
            var cfg = EcosystemConfig.ForIntegrationTests();
            cfg.EnableFernPhenology = true;
            cfg.EnableFernSporulationGate = false;
            cfg.StaggerReproduceAttempts = false;

            using var host = EcosystemSimHost.Create(cfg);
            var pos = new BlockPos(12, 64, 12);
            host.World
                .Chunk(0, 0)
                .RainHeight(12, 12, 64)
                .Column(12, 12, c => c.Soil(63).Eaglefern(64))
                .Build();

            host.LoadChunk(new Vec2i(0, 0));

            Assert.True(host.Eco.Test_TryGetRegistryEntry(pos, out _));

            host.TickReproduce(times: 3);

            Assert.True(host.Eco.Test_TryGetRegistryEntry(pos, out ReproducerEntry afterTick));
            Assert.Equal("eaglefern", afterTick.Requirements.Species);
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
