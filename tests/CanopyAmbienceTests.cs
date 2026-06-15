using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class CanopyAmbienceSeasonCurvesTests
    {
        [Theory]
        [InlineData(2, 0.2f)]
        [InlineData(4, 0.8f)]
        [InlineData(5, 1.0f)]
        [InlineData(11, 0f)]
        public void MoteRate_ByMonth(int month, float expected)
        {
            Assert.Equal(expected, CanopyAmbienceSeasonCurves.MoteRate(month));
        }

        [Theory]
        [InlineData(9, 1.0f)]
        [InlineData(10, 1.0f)]
        [InlineData(5, 0.1f)]
        [InlineData(11, 0.4f)]
        public void DriftRate_ByMonth(int month, float expected)
        {
            Assert.Equal(expected, CanopyAmbienceSeasonCurves.DriftRate(month));
        }

        [Theory]
        [InlineData(0.15f, true, 0f)]
        [InlineData(0.05f, true, 1f)]
        [InlineData(0.5f, false, 1f)]
        public void WeatherAttenuation_Rain(float rainfall, bool suppress, float expected)
        {
            Assert.Equal(expected, CanopyAmbienceSeasonCurves.WeatherAttenuation(rainfall, suppress));
        }

        [Fact]
        public void MonthFromYearProgress_June()
        {
            Assert.Equal(6, CanopyAmbienceSeasonCurves.MonthFromYearProgress(0.5f));
        }
    }
}
