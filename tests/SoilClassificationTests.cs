using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class SoilClassificationTests
    {
        [Fact]
        public void MeetsSoilRequirements_NullReq_ReturnsTrue()
        {
            Assert.True(SoilClassification.MeetsSoilRequirements(null, SoilKind.HighFert, 300));
        }

        [Fact]
        public void MeetsSoilRequirements_NoFilter_ReturnsTrue()
        {
            var req = new PlantRequirements { AllowedSoilKinds = SoilKind.None };
            Assert.True(SoilClassification.MeetsSoilRequirements(req, SoilKind.Sand, 50));
        }

        [Fact]
        public void MeetsSoilRequirements_AllowedSoilMatch()
        {
            var req = new PlantRequirements { AllowedSoilKinds = SoilKind.HighFert | SoilKind.MediumFert };
            Assert.True(SoilClassification.MeetsSoilRequirements(req, SoilKind.HighFert, 300));
        }

        [Fact]
        public void MeetsSoilRequirements_AllowedSoilMismatch()
        {
            var req = new PlantRequirements { AllowedSoilKinds = SoilKind.HighFert };
            Assert.False(SoilClassification.MeetsSoilRequirements(req, SoilKind.Sand, 50));
        }

        [Fact]
        public void MeetsSoilRequirements_MinFertility_Pass()
        {
            var req = new PlantRequirements { MinGroundFertility = 100 };
            Assert.True(SoilClassification.MeetsSoilRequirements(req, SoilKind.MediumFert, 200));
        }

        [Fact]
        public void MeetsSoilRequirements_MinFertility_Fail()
        {
            var req = new PlantRequirements { MinGroundFertility = 100 };
            Assert.False(SoilClassification.MeetsSoilRequirements(req, SoilKind.LowFert, 50));
        }

        [Fact]
        public void MeetsSoilRequirements_MaxFertility_Pass()
        {
            var req = new PlantRequirements { MaxGroundFertility = 300 };
            Assert.True(SoilClassification.MeetsSoilRequirements(req, SoilKind.MediumFert, 200));
        }

        [Fact]
        public void MeetsSoilRequirements_MaxFertility_Fail()
        {
            var req = new PlantRequirements { MaxGroundFertility = 100 };
            Assert.False(SoilClassification.MeetsSoilRequirements(req, SoilKind.HighFert, 300));
        }

        [Theory]
        [InlineData(0f, 0)]
        [InlineData(0.5f, 160)]
        [InlineData(1f, 320)]
        [InlineData(-0.1f, 0)]
        public void WorldgenFertilityToBlock_Maps(float worldgen, int expected)
        {
            Assert.Equal(expected, SoilClassification.WorldgenFertilityToBlock(worldgen));
        }

        [Fact]
        public void DescribeSoilFailure_NullReq_ReturnsNull()
        {
            Assert.Null(SoilClassification.DescribeSoilFailure(null, SoilKind.HighFert, 300));
        }

        [Fact]
        public void DescribeSoilFailure_SoilMismatch_ReturnsMessage()
        {
            var req = new PlantRequirements { AllowedSoilKinds = SoilKind.HighFert };
            string msg = SoilClassification.DescribeSoilFailure(req, SoilKind.Sand, 0);
            Assert.NotNull(msg);
            Assert.Contains("not allowed", msg);
        }

        [Fact]
        public void DescribeSoilFailure_TooLowFertility_ReturnsMessage()
        {
            var req = new PlantRequirements { MinGroundFertility = 200 };
            string msg = SoilClassification.DescribeSoilFailure(req, SoilKind.LowFert, 50);
            Assert.NotNull(msg);
            Assert.Contains("not fertile enough", msg);
        }

        [Fact]
        public void DescribeSoilFailure_TooRichFertility_ReturnsMessage()
        {
            var req = new PlantRequirements { MaxGroundFertility = 100 };
            string msg = SoilClassification.DescribeSoilFailure(req, SoilKind.HighFert, 300);
            Assert.NotNull(msg);
            Assert.Contains("too rich", msg);
        }

        [Fact]
        public void DescribeSoilFailure_AllOk_ReturnsNull()
        {
            var req = new PlantRequirements
            {
                AllowedSoilKinds = SoilKind.MediumFert,
                MinGroundFertility = 100,
                MaxGroundFertility = 300
            };
            Assert.Null(SoilClassification.DescribeSoilFailure(req, SoilKind.MediumFert, 200));
        }
    }
}
