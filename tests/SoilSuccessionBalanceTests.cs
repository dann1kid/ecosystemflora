using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SoilSuccessionBalanceTests
    {
        [Fact]
        public void MeadowColonizer_DeathImpact_IsHumusPositive()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                "wilddaisy", SoilSuccessionEvent.Death, out SoilImpact impact));
            Assert.True(impact.FertilityTierDelta > 0f);
        }

        [Fact]
        public void MeadowColonizer_SpreadImpact_IsFertilityPositive()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                "cornflower", SoilSuccessionEvent.Spread, out SoilImpact impact));
            Assert.True(impact.FertilityTierDelta > 0f);
        }

        [Fact]
        public void SoilDepleter_SpreadImpact_IsFertilityNegative()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                "heather", SoilSuccessionEvent.Spread, out SoilImpact impact));
            Assert.True(impact.FertilityTierDelta < 0f);
        }

        [Fact]
        public void SoilDepleter_DeathImpact_StillReturnsSomeOrganicMatter()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact(
                "heather", SoilSuccessionEvent.Death, out SoilImpact impact));
            Assert.True(impact.FertilityTierDelta > 0f);
        }
    }
}
