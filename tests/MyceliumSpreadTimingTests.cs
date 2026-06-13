using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumSpreadTimingTests
    {
        [Fact]
        public void EffectiveChance_ScalesWithSpreadRate()
        {
            var cfg = new EcosystemConfig
            {
                ReproduceChance = 0.5f,
                MyceliumSpreadRate = 0.12f,
            };
            var req = new PlantRequirements { SpreadRate = 0.12f };

            float chance = MyceliumSpreadTiming.EffectiveChance(cfg, req);

            Assert.Equal(0.06f, chance, precision: 3);
        }

        [Fact]
        public void MyceliumPlacement_MinFertility_IsVanillaAligned()
        {
            Assert.Equal(10, MyceliumPlacement.MinGroundFertility);
        }
    }
}
