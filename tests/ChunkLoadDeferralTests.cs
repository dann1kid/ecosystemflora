using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ChunkLoadDeferralTests
    {
        [Fact]
        public void DelayMs_ZeroWindow_ReturnsBase()
        {
            var coord = new Vec2i(3, 7);
            Assert.Equal(250, ChunkLoadDeferral.DelayMs(coord, 250, 0));
            Assert.Equal(250, ChunkLoadDeferral.DelayMs(coord, 250, -1));
        }

        [Fact]
        public void DelayMs_SameCoord_IsStable()
        {
            var coord = new Vec2i(12, -4);
            int a = ChunkLoadDeferral.DelayMs(coord, 500, 1500);
            int b = ChunkLoadDeferral.DelayMs(coord, 500, 1500);
            Assert.Equal(a, b);
        }

        [Fact]
        public void DelayMs_InHalfOpenRange()
        {
            const int baseMs = 250;
            const int window = 2000;
            for (int i = 0; i < 64; i++)
            {
                int delay = ChunkLoadDeferral.DelayMs(new Vec2i(i, i * 3), baseMs, window);
                Assert.InRange(delay, baseMs, baseMs + window - 1);
            }
        }

        [Fact]
        public void DelayMs_DifferentCoords_UsuallySpread()
        {
            var delays = new System.Collections.Generic.HashSet<int>();
            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    delays.Add(ChunkLoadDeferral.DelayMs(new Vec2i(x, z), 200, 1500));
                }
            }

            // Not all 1024 columns should collapse to one delay bucket.
            Assert.True(delays.Count > 50, $"expected spread, got {delays.Count} distinct delays");
        }

        [Fact]
        public void NamedDelays_UsePlanBases()
        {
            var coord = new Vec2i(1, 2);
            Assert.InRange(
                ChunkLoadDeferral.StripDelayMs(coord),
                ChunkLoadDeferral.StripBaseMs,
                ChunkLoadDeferral.StripBaseMs + ChunkLoadDeferral.StripStaggerWindowMs - 1);
            Assert.InRange(
                ChunkLoadDeferral.RemapDelayMs(coord),
                ChunkLoadDeferral.RemapBaseMs,
                ChunkLoadDeferral.RemapBaseMs + ChunkLoadDeferral.RemapStaggerWindowMs - 1);
            Assert.InRange(
                ChunkLoadDeferral.MyceliumDelayMs(coord),
                ChunkLoadDeferral.MyceliumBaseMs,
                ChunkLoadDeferral.MyceliumBaseMs + ChunkLoadDeferral.MyceliumStaggerWindowMs - 1);
            Assert.InRange(
                ChunkLoadDeferral.RegistrationDelayMs(coord),
                ChunkLoadDeferral.RegistrationBaseMs,
                ChunkLoadDeferral.RegistrationBaseMs + ChunkLoadDeferral.RegistrationStaggerWindowMs - 1);
        }

        [Fact]
        public void ShouldBurstOnLoad_False_WhenBackgroundScanEnabled()
        {
            var cfg = new EcosystemConfig
            {
                EnableBurstRegistrationNearPlayers = true,
                EnableBackgroundRegistrationScan = true,
            };
            Assert.False(ChunkLoadDeferral.ShouldBurstOnLoad(cfg));
        }

        [Fact]
        public void ShouldBurstOnLoad_True_OnlyForSyncClassifyPath()
        {
            var cfg = new EcosystemConfig
            {
                EnableBurstRegistrationNearPlayers = true,
                EnableBackgroundRegistrationScan = false,
            };
            Assert.True(ChunkLoadDeferral.ShouldBurstOnLoad(cfg));

            cfg.EnableBurstRegistrationNearPlayers = false;
            Assert.False(ChunkLoadDeferral.ShouldBurstOnLoad(cfg));
            Assert.False(ChunkLoadDeferral.ShouldBurstOnLoad(null));
        }
    }
}
