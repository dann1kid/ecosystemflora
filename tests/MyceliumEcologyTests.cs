using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class MyceliumEcologyTests
    {
        [Theory]
        [InlineData("mushroom-kingbolete-normal-north", "kingbolete")]
        [InlineData("mushroom-fieldmushroom-normal-up", "fieldmushroom")]
        [InlineData("mushroom-orangeoakbolete-normal-up", "orangeoakbolete")]
        public void ParseMushroomType_ExtractsTypeSegment(string path, string expected)
        {
            string type = MyceliumEcology.ParseMushroomType(new AssetLocation("game", path));
            Assert.Equal(expected, type);
        }

        [Fact]
        public void ClassifyNiche_FieldMushroom_IsMeadowOpen()
        {
            var code = new AssetLocation("game", "mushroom-fieldmushroom-normal-up");
            MyceliumNiche niche = MyceliumEcology.ClassifyNiche(code, null);
            Assert.Equal(MyceliumNiche.MeadowOpen, niche);
        }

        [Fact]
        public void ClassifyNiche_OrangeOakBolete_IsDeciduous()
        {
            var code = new AssetLocation("game", "mushroom-orangeoakbolete-normal-up");
            MyceliumNiche niche = MyceliumEcology.ClassifyNiche(code, null);
            Assert.Equal(MyceliumNiche.ForestDeciduous, niche);
        }

        [Fact]
        public void ClassifyNiche_LogAnchor_IsTrunkPolypore()
        {
            var code = new AssetLocation("game", "mushroom-shiitake-side-north");
            var log = new Block { Code = new AssetLocation("game", "log-grown-oak-ud") };
            MyceliumNiche niche = MyceliumEcology.ClassifyNiche(code, log);
            Assert.Equal(MyceliumNiche.TrunkPolypore, niche);
        }

        [Fact]
        public void TryBuildRequirements_SetsMyceliumAnchorHabitat()
        {
            var code = new AssetLocation("game", "mushroom-chanterelle-normal-up");
            Assert.True(MyceliumEcology.TryBuildRequirements(code, null, out PlantRequirements req));
            Assert.Equal(EcologyHabitat.MyceliumAnchor, req.Habitat);
            Assert.Equal(SpreadMode.MyceliumNetwork, req.SpreadMode);
            Assert.Equal("chanterelle", req.Species);
            Assert.Equal(0.12f, req.SpreadRate);
        }
    }
}
