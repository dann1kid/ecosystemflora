using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemPerfConfigTests
    {
        [Fact]
        public void LimitSpreadNearPlayers_DefaultsFalse()
        {
            var cfg = new EcosystemConfig();
            Assert.False(cfg.LimitSpreadNearPlayers);
        }

        [Fact]
        public void ReproduceTickProfiling_DefaultsOffUntilLargeRegistry()
        {
            var cfg = new EcosystemConfig();
            Assert.False(cfg.EnableReproduceTickProfiling);
            Assert.Equal(2000, cfg.ReproduceTickProfilingMinRegistry);
            Assert.Equal(30000, cfg.ReproduceTickProfilingIntervalMs);
        }

        [Fact]
        public void Phase6Spread_DefaultsEnableChunkFairAndEventWake()
        {
            var cfg = new EcosystemConfig();
            Assert.True(cfg.EnableChunkFairSpread);
            Assert.True(cfg.EnableEventDrivenSpread);
            Assert.True(cfg.EnableBackgroundSpreadSolve);
            Assert.True(cfg.EnableSeasonCoarseWake);
            Assert.Equal(2, cfg.MaxSpreadAttemptsPerChunkPerTick);
            Assert.Equal(32, cfg.MaxSpreadChunksVisitedPerTick);
        }
    }
}
