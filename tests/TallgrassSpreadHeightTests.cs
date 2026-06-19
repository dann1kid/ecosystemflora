using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TallgrassSpreadHeightTests
    {
        [Theory]
        [InlineData("tallgrass-fern-veryshort-free", "tallgrass", "fern", "veryshort", true, false)]
        [InlineData("tallgrass-verytall-free", "tallgrass", null, "verytall", true, false)]
        [InlineData("frostedtallgrass-fern-free", "frostedtallgrass", "fern", null, true, false)]
        [InlineData("frostedtallgrass-tall-free", "frostedtallgrass", null, "tall", true, false)]
        [InlineData("tallgrass-fern-mediumshort-snow", "tallgrass", "fern", "mediumshort", false, true)]
        public void TryParsePath_ReadsCoverHeightAndSuffix(
            string path,
            string prefix,
            string cover,
            string height,
            bool hasFree,
            bool hasSnow)
        {
            Assert.True(TallgrassSpreadHeight.TryParsePath(path, out var parts));
            Assert.Equal(prefix, parts.Prefix);
            Assert.Equal(cover, parts.Cover);
            Assert.Equal(height, parts.Height);
            Assert.Equal(hasFree, parts.HasFree);
            Assert.Equal(hasSnow, parts.HasSnow);
        }

        [Theory]
        [InlineData("tallgrass", "fern", "short", true, false, "tallgrass-fern-short-free")]
        [InlineData("tallgrass", null, "medium", true, false, "tallgrass-medium-free")]
        [InlineData("frostedtallgrass", "fern", "veryshort", true, false, "frostedtallgrass-fern-veryshort-free")]
        [InlineData("tallgrass", "fern", "tall", false, true, "tallgrass-fern-tall-snow")]
        public void BuildPath_AssemblesVanillaCodes(
            string prefix,
            string cover,
            string height,
            bool hasFree,
            bool hasSnow,
            string expected)
        {
            var parts = new TallgrassSpreadHeight.TallgrassPathParts(prefix, cover, height, hasFree, hasSnow);
            Assert.Equal(expected, TallgrassSpreadHeight.BuildPath(parts));
        }

        [Fact]
        public void PickStageIndex_OpenSunlight_TendsTallerThanDeepShade()
        {
            var open = new TallgrassSpreadHeight.TallgrassHeightContext(
                sunLightLevel: 20,
                localForestCover: 0.05f,
                groundFertility: 85,
                worldgenRainfall: 0.6f,
                nicheLight: LightLevel.Open,
                nicheMoisture: MoistureLevel.Mesic,
                seasonGrowthFactor: 1.4f);

            var shade = new TallgrassSpreadHeight.TallgrassHeightContext(
                sunLightLevel: 6,
                localForestCover: 0.85f,
                groundFertility: 40,
                worldgenRainfall: 0.35f,
                nicheLight: LightLevel.DeepShade,
                nicheMoisture: MoistureLevel.Dry,
                seasonGrowthFactor: 0.2f);

            var rand = new System.Random(42);
            int openIdx = TallgrassSpreadHeight.PickStageIndex(in open, rand);
            int shadeIdx = TallgrassSpreadHeight.PickStageIndex(in shade, rand);

            Assert.True(openIdx > shadeIdx);
            Assert.True(shadeIdx <= 2);
        }

        [Fact]
        public void PickStageIndex_JitterProducesHeightVariety()
        {
            var ctx = new TallgrassSpreadHeight.TallgrassHeightContext(
                sunLightLevel: 14,
                localForestCover: 0.35f,
                groundFertility: 70,
                worldgenRainfall: 0.5f,
                nicheLight: LightLevel.Partial,
                nicheMoisture: MoistureLevel.Mesic,
                seasonGrowthFactor: 1.0f);

            var seen = new System.Collections.Generic.HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                seen.Add(TallgrassSpreadHeight.PickStageIndex(in ctx, new System.Random(seed)));
            }

            Assert.True(seen.Count >= 3);
        }
    }
}
