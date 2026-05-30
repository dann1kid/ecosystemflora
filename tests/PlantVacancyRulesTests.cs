using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PlantVacancyRulesTests
    {
        [Theory]
        [InlineData(int.MaxValue, true)]
        [InlineData(6000, true)]
        [InlineData(5000, true)]
        [InlineData(4999, false)]
        [InlineData(0, false)]
        public void MeetsMinReplaceable_UsesSpreadThreshold(int replaceable, bool expected)
        {
            Assert.Equal(
                expected,
                PlantVacancyRules.MeetsMinReplaceable(replaceable, SuitabilityEvaluator.ReproduceMinReplaceable));
        }

        [Fact]
        public void IsVacantPlantSpace_NullIsNotVacant()
        {
            Assert.False(PlantVacancyRules.IsVacantPlantSpace(null));
        }
    }
}
