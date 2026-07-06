using Moq;
using Vintagestory.API.Common;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    public class CalendarSpeedHelperTests
    {
        [Theory]
        [InlineData(0.13f)]
        [InlineData(0.5f)]
        [InlineData(2f)]
        public void ScaleCalendarHours_InverselyScalesBySpeedMultiplier(float speed)
        {
            var cal = new EcologyTestCalendar { CalendarSpeedMul = speed, SpeedOfTime = speed };
            double baseHours = 24;

            double scaled = CalendarSpeedHelper.ScaleCalendarHours(baseHours, cal);

            Assert.Equal(baseHours / speed, scaled, 3);
        }

        [Fact]
        public void EffectiveIntervalHours_SlowCalendar_LengthensSpreadInterval()
        {
            var cal = new EcologyTestCalendar
            {
                CalendarSpeedMul = 0.13f,
                SpeedOfTime = 0.13f,
                DaysPerYear = 9,
                HoursPerDay = 24,
            };
            var cfg = new EcosystemConfig
            {
                UseCalendarScaledSpread = true,
                ReproduceAttemptsPerYear = 72,
                UseSpeciesSpreadRates = false,
            };
            var req = new PlantRequirements { SpreadRate = 1f };

            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.Calendar).Returns(cal);
            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);

            cal.CalendarSpeedMul = 1f;
            cal.SpeedOfTime = 1f;
            double atNormalSpeed = SpeciesSpread.EffectiveIntervalHours(api.Object, null, cfg, req);

            cal.CalendarSpeedMul = 0.13f;
            cal.SpeedOfTime = 0.13f;
            double atSlowSpeed = SpeciesSpread.EffectiveIntervalHours(api.Object, null, cfg, req);

            Assert.True(atSlowSpeed > atNormalSpeed);
            Assert.Equal(atNormalSpeed / 0.13, atSlowSpeed, atNormalSpeed * 0.02);
        }
    }
}
