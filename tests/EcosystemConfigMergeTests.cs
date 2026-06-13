using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemConfigMergeTests
    {
        [Fact]
        public void FreshConfig_HasMyceliumDefaults()
        {
            var cfg = new EcosystemConfig();

            Assert.True(cfg.EnableMyceliumNiche);
            Assert.True(cfg.EnableMyceliumEcology);
            Assert.True(cfg.EnableMyceliumNetworkSpread);
            Assert.Equal(7, cfg.MyceliumZoneRadius);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void ShouldPersistConfig_WhenFileExistsOrServerCreates(
            bool createDefaultIfMissing,
            bool fileExisted,
            bool expected)
        {
            Assert.Equal(expected, EcosystemConfig.ShouldPersistConfig(createDefaultIfMissing, fileExisted));
        }
    }
}
