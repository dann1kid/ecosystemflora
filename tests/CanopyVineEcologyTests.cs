using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class CanopyTreeAgeBoostTests
    {
        [Theory]
        [InlineData(0, 1f)]
        [InlineData(30, 1.25f)]
        [InlineData(60, 1.5f)]
        [InlineData(120, 1.5f)]
        public void SpringBranchyBudMultiplier_ScalesWithAge(int ageYears, float expected)
        {
            EcosystemConfig.Loaded = new EcosystemConfig
            {
                EnableSpringBranchyAgeBoost = true,
                SpringBranchyAgeBoostYearsToMax = 60f,
                SpringBranchyAgeBoostMax = 1.5f,
            };

            float mult = CanopyTreeAgeBoost.SpringBranchyBudMultiplierForAge(ageYears);
            Assert.Equal(expected, mult, precision: 3);
        }
    }

    public class WildVineHelperTests
    {
        [Theory]
        [InlineData("wildvine-end-north", false, true, "north")]
        [InlineData("wildvine-section-east", false, false, "east")]
        [InlineData("wildvine-tropical-end-south", true, true, "south")]
        [InlineData("wildvine-tropical-section-west", true, false, "west")]
        public void TryParse_RecognisesVanillaPaths(string path, bool tropical, bool isEnd, string facingCode)
        {
            var block = new Block { Code = new AssetLocation("game", path) };

            Assert.True(WildVineHelper.TryParse(block, out WildVineInfo info));
            Assert.Equal(tropical, info.Tropical);
            Assert.Equal(isEnd, info.IsEnd);
            Assert.Equal(facingCode, info.Facing.Code);
        }

        [Fact]
        public void HostPosAndVinePosForHost_AreInverse()
        {
            var host = new BlockPos(10, 64, 10);
            BlockFacing facing = BlockFacing.NORTH;

            BlockPos vine = WildVineHelper.VinePosForHost(host, facing);
            BlockPos back = WildVineHelper.HostPos(vine, facing);

            Assert.Equal(host, back);
        }

        [Theory]
        [InlineData("wildvine", true)]
        [InlineData("wildvine-tropical", true)]
        [InlineData("oak", false)]
        public void EcologySpecies_RecognisesVines(string species, bool expected)
        {
            Assert.Equal(expected, WildVineEcology.IsSpecies(species));
        }
    }
}
