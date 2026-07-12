using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using WildFarming.Tests.Harness;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassEstablishmentInspectTests
    {
        [Fact]
        public void TryBuild_QueuedVeryshort_ReturnsGrowing()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                using EcosystemSimHost host = EcosystemSimHost.Create(new EcosystemConfig
                {
                    EnableTallgrassSpreadMaturation = true,
                });

                Block[] blocks = EcologyTestBlocks.CreateCatalog();
                var pos = new BlockPos(8, 64, 8);
                host.Accessor.SetBlock(blocks[4].BlockId, pos);
                host.Eco.Test_AddTallgrassPromotion(pos);

                Assert.True(TallgrassEstablishmentInspect.TryBuild(
                    host.Api.Object,
                    pos,
                    blocks[4],
                    host.Eco,
                    out TallgrassEstablishmentInspect.Snapshot snap));
                Assert.Equal(TallgrassEstablishmentInspect.Phase.Growing, snap.Phase);
                Assert.True(snap.RegisterStageIndex > snap.CurrentStageIndex);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void TryBuild_TallEnough_ReturnsRegistrationPending()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                using EcosystemSimHost host = EcosystemSimHost.Create(new EcosystemConfig
                {
                    EnableTallgrassSpreadMaturation = true,
                });

                Block[] blocks = EcologyTestBlocks.CreateCatalog();
                var pos = new BlockPos(8, 64, 8);
                host.Accessor.SetBlock(blocks[6].BlockId, pos);

                Assert.True(TallgrassEstablishmentInspect.TryBuild(
                    host.Api.Object,
                    pos,
                    blocks[6],
                    host.Eco,
                    out TallgrassEstablishmentInspect.Snapshot snap));
                Assert.Equal(TallgrassEstablishmentInspect.Phase.RegistrationPending, snap.Phase);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }

        [Fact]
        public void TryBuild_UnqueuedVeryshort_ReturnsWaitingForScan()
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            try
            {
                using EcosystemSimHost host = EcosystemSimHost.Create(new EcosystemConfig
                {
                    EnableTallgrassSpreadMaturation = true,
                });

                Block[] blocks = EcologyTestBlocks.CreateCatalog();
                var pos = new BlockPos(8, 64, 8);
                host.Accessor.SetBlock(blocks[4].BlockId, pos);

                Assert.True(TallgrassEstablishmentInspect.TryBuild(
                    host.Api.Object,
                    pos,
                    blocks[4],
                    host.Eco,
                    out TallgrassEstablishmentInspect.Snapshot snap));
                Assert.Equal(TallgrassEstablishmentInspect.Phase.WaitingForScan, snap.Phase);
            }
            finally
            {
                EcosystemConfig.Loaded = prior;
            }
        }
    }
}
