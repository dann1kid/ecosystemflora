using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeSpreadMaturityTests
    {
        [Theory]
        [InlineData(12, 4, "birch", true)]
        [InlineData(14, 5, "oak", true)]
        [InlineData(8, 3, "birch", true)]
        [InlineData(1, 1, "birch", false)]
        [InlineData(3, 1, "birch", false)]
        public void MeetsStructuralBypass_worldgenSizedTrees(
            int trunkHeight,
            int crownRadius,
            string wood,
            bool expected)
        {
            var metrics = new TreeStructureMetrics(trunkHeight, crownRadius, null);
            var profile = WildTreeGrowthProfiles.Resolve(wood);
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };

            bool actual = TreeSpreadMaturity.MeetsStructuralBypass(metrics, profile, cfg);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AllowsSpread_ecologySeedling_blockedUntilCalendarMaturity()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };
            // Grown past soft bypass size (~55% of ref 12) but still young calendar age.
            var metrics = new TreeStructureMetrics(trunkHeight: 8, crownRadius: 3, trunkTop: null);
            int maturity = profile.SpreadMaturityAgeYears;
            int estimated = 10; // below maturity

            bool allowed = TreeSpreadMaturity.AllowsSpread(
                treeAgeYears: 5,
                structuralBypassEligible: false,
                estimatedAgeYears: estimated,
                metrics,
                profile,
                cfg,
                maturity);

            Assert.False(allowed);
        }

        [Fact]
        public void AllowsSpread_ecologySeedling_allowedAtCalendarMaturity()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };
            var metrics = new TreeStructureMetrics(trunkHeight: 8, crownRadius: 3, trunkTop: null);
            int maturity = profile.SpreadMaturityAgeYears;

            bool allowed = TreeSpreadMaturity.AllowsSpread(
                treeAgeYears: maturity,
                structuralBypassEligible: false,
                estimatedAgeYears: 0,
                metrics,
                profile,
                cfg,
                maturity);

            Assert.True(allowed);
        }

        [Fact]
        public void AllowsSpread_ecologySeedling_allowedWhenStructureEstimatesFullMaturity()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };
            var metrics = new TreeStructureMetrics(trunkHeight: 12, crownRadius: 4, trunkTop: null);
            int maturity = profile.SpreadMaturityAgeYears;

            bool allowed = TreeSpreadMaturity.AllowsSpread(
                treeAgeYears: 4,
                structuralBypassEligible: false,
                estimatedAgeYears: maturity,
                metrics,
                profile,
                cfg,
                maturity);

            Assert.True(allowed);
        }

        [Fact]
        public void AllowsSpread_worldgenAtRegistration_softBypassWhenEligible()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };
            var metrics = new TreeStructureMetrics(trunkHeight: 8, crownRadius: 3, trunkTop: null);
            int maturity = profile.SpreadMaturityAgeYears;

            bool allowed = TreeSpreadMaturity.AllowsSpread(
                treeAgeYears: 0,
                structuralBypassEligible: true,
                estimatedAgeYears: 10,
                metrics,
                profile,
                cfg,
                maturity);

            Assert.True(allowed);
        }

        [Fact]
        public void AllowsSpread_worldgenSoftBypass_stillWorksAfterAging()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var cfg = new EcosystemConfig { TreeYoungSpreadBypassTrunkHeight = 14 };
            var metrics = new TreeStructureMetrics(trunkHeight: 8, crownRadius: 3, trunkTop: null);
            int maturity = profile.SpreadMaturityAgeYears;

            bool allowed = TreeSpreadMaturity.AllowsSpread(
                treeAgeYears: 3,
                structuralBypassEligible: true,
                estimatedAgeYears: 10,
                metrics,
                profile,
                cfg,
                maturity);

            Assert.True(allowed);
        }

        [Fact]
        public void YearsUntilCalendarMaturity_countsRemaining()
        {
            Assert.Equal(10, TreeSpreadMaturity.YearsUntilCalendarMaturity(5, 15));
            Assert.Equal(0, TreeSpreadMaturity.YearsUntilCalendarMaturity(15, 15));
            Assert.Equal(0, TreeSpreadMaturity.YearsUntilCalendarMaturity(20, 15));
        }
    }
}
