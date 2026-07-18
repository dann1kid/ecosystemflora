using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeGrowthTargetsTests
    {
        [Fact]
        public void Oak_WorldgenSizedTree_IsNearReferenceIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(14, 7, profile);

            Assert.InRange(pct, 95, 105);
        }

        [Fact]
        public void Oak_TallTree_ExceedsReferenceIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(28, 10, profile);

            Assert.True(pct > 130);
        }

        [Fact]
        public void Oak_Sapling_IsLowSizeIndex()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            int pct = TreeGrowthTargets.SizeIndexPercent(3, 1, profile);

            Assert.InRange(pct, 10, 30);
        }

        [Fact]
        public void SenescenceAge_IsCalendarHorizon_NotStructure()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");

            Assert.True(profile.SenescenceAgeYears >= 80);
            Assert.True(profile.ReferenceTrunkHeight < 20);
            Assert.True(profile.ReferenceCrownRadius >= 6);
        }

        [Theory]
        [InlineData(10, 3, 0.5f)]
        [InlineData(14, 7, 0.95f)]
        public void SizeIndexFraction_ScalesWithStructure(int trunk, int crown, float expectedMin)
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            float fraction = TreeGrowthTargets.SizeIndexFraction(trunk, crown, profile);

            Assert.InRange(fraction, expectedMin, 2.5f);
        }

        [Theory]
        [InlineData(1.0f, 0.4f, true)]
        [InlineData(0.9f, 0.5f, true)]
        [InlineData(0.8f, 0.9f, false)]
        [InlineData(0.4f, 0.3f, false)]
        [InlineData(1.0f, 1.0f, false)]
        public void CrownLagsTrunk_DetectsTallSkinnyTrees(float trunkVs, float crownVs, bool expected)
        {
            Assert.Equal(expected, TreeGrowthTargets.CrownLagsTrunk(trunkVs, crownVs));
        }

        [Fact]
        public void OrderCandidatesUpperCrownFirst_PutsTopAnchorsAhead()
        {
            var metrics = new TreeStructureMetrics(12, 4, new BlockPos(0, 72, 0));
            var candidates = new List<BlockPos>
            {
                new BlockPos(1, 64, 0),
                new BlockPos(1, 71, 0),
                new BlockPos(2, 68, 0),
            };

            TreeGrowthApplier.OrderCandidatesUpperCrownFirst(candidates, metrics);

            Assert.Equal(71, candidates[0].Y);
            Assert.True(candidates[0].Y >= candidates[1].Y);
            Assert.True(candidates[1].Y >= candidates[2].Y);
        }

        [Theory]
        [InlineData("oak", TreeCrownForm.Spreading)]
        [InlineData("maple", TreeCrownForm.Spreading)]
        [InlineData("walnut", TreeCrownForm.Spreading)]
        [InlineData("birch", TreeCrownForm.Oval)]
        [InlineData("acacia", TreeCrownForm.Umbrella)]
        [InlineData("kapok", TreeCrownForm.Umbrella)]
        [InlineData("pine", TreeCrownForm.Column)]
        [InlineData("redwood", TreeCrownForm.Tiered)]
        public void Resolve_AssignsExpectedCrownForm(string wood, TreeCrownForm expected)
        {
            Assert.Equal(expected, WildTreeGrowthProfiles.Resolve(wood).CrownForm);
        }

        [Fact]
        public void SpreadingEnvelope_IsWiderNearTipThanNearBole()
        {
            var profile = WildTreeGrowthProfiles.Resolve("oak");
            var trunkBase = new BlockPos(0, 60, 0);
            var metrics = new TreeStructureMetrics(14, 4, new BlockPos(0, 73, 0));

            int nearBole = TreeCrownEnvelope.AllowedRadiusAtY(profile, metrics, trunkBase, 68);
            int nearTip = TreeCrownEnvelope.AllowedRadiusAtY(profile, metrics, trunkBase, 73);

            Assert.True(nearTip > nearBole);
            Assert.False(TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, new BlockPos(5, 64, 0)));
            Assert.True(TreeCrownEnvelope.AllowsCell(profile, metrics, trunkBase, new BlockPos(4, 73, 0)));
        }

        [Fact]
        public void OvalEnvelope_PrefersMidCrownOverTipShelf()
        {
            var profile = WildTreeGrowthProfiles.Resolve("birch");
            var trunkBase = new BlockPos(0, 60, 0);
            var tip = new BlockPos(0, 71, 0);
            var metrics = new TreeStructureMetrics(12, 3, tip);

            int midPriority = TreeCrownEnvelope.AnchorPriority(
                profile.CrownForm, metrics, trunkBase, new BlockPos(1, 68, 0));
            int tipPriority = TreeCrownEnvelope.AnchorPriority(
                profile.CrownForm, metrics, trunkBase, new BlockPos(1, 71, 0));

            Assert.True(midPriority > tipPriority);
        }

        [Fact]
        public void UmbrellaEnvelope_StaysTightBelowHighShelf()
        {
            var profile = WildTreeGrowthProfiles.Resolve("acacia");
            var trunkBase = new BlockPos(0, 60, 0);
            var metrics = new TreeStructureMetrics(10, 3, new BlockPos(0, 69, 0));

            int low = TreeCrownEnvelope.AllowedRadiusAtY(profile, metrics, trunkBase, 64);
            int high = TreeCrownEnvelope.AllowedRadiusAtY(profile, metrics, trunkBase, 69);

            Assert.True(high >= low + 1);
            Assert.True(low <= 2);
        }
    }
}

