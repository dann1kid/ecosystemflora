using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class RegistrationBlockMatchTests
    {
        [Fact]
        public void MatchesSnapshot_AllowsCoverVariantDrift()
        {
            var free = new Block { BlockId = 1, Code = new AssetLocation("game:fern-eaglefern-free") };
            var snow = new Block { BlockId = 2, Code = new AssetLocation("game:fern-eaglefern-snow") };

            Assert.True(RegistrationBlockMatch.MatchesSnapshot(snow, free.Code));
        }

        [Fact]
        public void MatchesSnapshot_AllowsVanillaToPhaseDrift()
        {
            var vanilla = new Block { BlockId = 1, Code = new AssetLocation("game:fern-eaglefern-free") };
            var phase = new Block { BlockId = 2, Code = new AssetLocation("ecosystemflora:fernphase-eaglefern-dormant-free") };

            Assert.True(RegistrationBlockMatch.MatchesSnapshot(phase, vanilla.Code));
        }

        [Fact]
        public void MatchesSnapshot_RejectsDifferentSpecies()
        {
            var eagle = new Block { BlockId = 1, Code = new AssetLocation("game:fern-eaglefern-free") };
            var cinnamon = new Block { BlockId = 2, Code = new AssetLocation("game:fern-cinnamonfern-free") };

            Assert.False(RegistrationBlockMatch.MatchesSnapshot(cinnamon, eagle.Code));
        }
    }
}
