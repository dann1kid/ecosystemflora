using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class WildSpeciesModifiersTests
    {
        [Fact]
        public void Colonizers_WeakerHoldThan_ClimaxPerennials()
        {
            Assert.True(TryHold("horsetail", out float colonizer));
            Assert.True(TryHold("daffodil", out float climax));
            Assert.True(colonizer < climax);
            Assert.True(colonizer < 0.75f);
            Assert.True(climax >= 1.15f);
        }

        [Fact]
        public void Tallgrass_WeakerHoldThan_MeadowFlowers()
        {
            Assert.True(TryHold("tallgrass", out float grass));
            Assert.True(TryHold("cornflower", out float flower));
            Assert.True(grass < flower);
        }

        [Fact]
        public void ForestUnderstory_StrongerHoldThan_OpenColonizers()
        {
            Assert.True(TryHold("lupine", out float lupine));
            Assert.True(TryHold("lilyofthevalley", out float lily));
            Assert.True(lily > lupine + 0.5f);
        }

        [Fact]
        public void FastSpreadColonizer_LowerHoldThan_SlowRare()
        {
            Assert.True(WildFlowerClimate.TryGet("horsetail", out var fast));
            Assert.True(WildFlowerClimate.TryGet("goldenpoppy", out var slow));
            Assert.True(TryHold("horsetail", out float fastHold));
            Assert.True(TryHold("goldenpoppy", out float slowHold));
            Assert.True(fast.SpreadRate > slow.SpreadRate);
            Assert.True(fastHold < slowHold);
        }

        static bool TryHold(string species, out float hold)
        {
            hold = 0f;
            if (!WildSpeciesModifiers.TryGet(species, out var profile)) return false;
            hold = profile.HoldStrength;
            return true;
        }
    }
}
