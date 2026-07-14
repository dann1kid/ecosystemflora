using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class ColumnTrafficStoreTests
    {
        [Fact]
        public void AddPressure_accumulates_up_to_255()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(10, 64, 20);

            byte p1 = store.AddPressure(pos, 200, nowHours: 0, hoursPerDay: 24, decayPerDay: 0);
            Assert.Equal(200, p1);

            byte p2 = store.AddPressure(pos, 100, nowHours: 1, hoursPerDay: 24, decayPerDay: 0);
            Assert.Equal(255, p2);
        }

        [Fact]
        public void LazyDecay_reduces_pressure_over_game_days()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(3, 70, 4);

            store.AddPressure(pos, 120, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            float after = store.GetPressure01(pos, nowHours: 24f * 10f, hoursPerDay: 24f, decayPerDay: 6f);
            // 10 days × 6 = 60 points → 120-60 = 60 → ~0.235
            Assert.InRange(after, 0.22f, 0.25f);
        }

        [Fact]
        public void LazyDecay_clears_record_at_zero()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(1, 1, 1);

            store.AddPressure(pos, 12, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            float after = store.GetPressure01(pos, nowHours: 24f * 3f, hoursPerDay: 24f, decayPerDay: 6f);
            Assert.Equal(0f, after);
            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void SoilWear_fires_each_pressure_step()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(5, 5, 5);

            store.AddPressure(pos, 40, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            Assert.True(store.TryConsumeSoilWear(pos, 40, wearStep: 40, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f));
            Assert.False(store.TryConsumeSoilWear(pos, 40, wearStep: 40, nowHours: 1, hoursPerDay: 24f, decayPerDay: 0f));

            store.AddPressure(pos, 40, nowHours: 2, hoursPerDay: 24f, decayPerDay: 0f);
            Assert.True(store.TryConsumeSoilWear(pos, 80, wearStep: 40, nowHours: 2, hoursPerDay: 24f, decayPerDay: 0f));
        }

        [Fact]
        public void SoilWear_catches_up_when_pressure_jumps()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(6, 6, 6);

            store.AddPressure(pos, 255, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            int wears = 0;
            while (store.TryConsumeSoilWear(pos, 255, wearStep: 40, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f))
            {
                wears++;
                Assert.True(wears <= 8);
            }

            // 40,80,120,160,200,240 → 6 wears (280 would exceed 255 mark chain)
            Assert.Equal(6, wears);
        }

        [Fact]
        public void PlantHits_increment_and_clear()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(8, 8, 8);

            Assert.Equal(1, store.IncrementPlantHits(pos, 0, 24f, 0f));
            Assert.Equal(2, store.IncrementPlantHits(pos, 1, 24f, 0f));
            store.ClearPlantHits(pos);
            Assert.Equal(1, store.IncrementPlantHits(pos, 2, 24f, 0f));
        }
        [Fact]
        public void AgeAllAndPrune_decays_and_drops_zero_records()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(9, 9, 9);
            store.AddPressure(pos, 12, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            Assert.Equal(1, store.Count);

            store.AgeAllAndPrune(
                api: null,
                nowHours: 24f * 3f,
                hoursPerDay: 24f,
                decayPerDay: 6f,
                syncCoverage: false,
                wearStep: 80);

            Assert.Equal(0, store.Count);
        }

        [Fact]
        public void AgeAllAndPrune_keeps_zero_pressure_until_soil_mark_cleared()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(11, 11, 11);
            store.AddPressure(pos, 12, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            store.SetLastSoilPressure(pos, 160);

            store.AgeAllAndPrune(
                api: null,
                nowHours: 24f * 3f,
                hoursPerDay: 24f,
                decayPerDay: 6f,
                syncCoverage: false,
                wearStep: 80);

            Assert.Equal(1, store.Count);
            Assert.True(store.TryGetRecordSnapshot(pos, 24f * 3f, 24f, 0f, out byte p, out _, out byte last));
            Assert.Equal(0, p);
            Assert.Equal(160, last);
        }

        [Fact]
        public void AgeAllAndPrune_skips_chunk_work_when_soil_mark_matches_target()
        {
            var store = new ColumnTrafficStore();
            var pos = new Vintagestory.API.MathTools.BlockPos(12, 12, 12);

            // Pressure 80 → target mark 80 with wearStep 80; set mark already aligned.
            store.AddPressure(pos, 80, nowHours: 0, hoursPerDay: 24f, decayPerDay: 0f);
            store.SetLastSoilPressure(pos, 80);

            store.AgeAllAndPrune(
                api: null,
                nowHours: 1,
                hoursPerDay: 24f,
                decayPerDay: 0f,
                syncCoverage: true,
                wearStep: 80);

            Assert.True(store.TryGetRecordSnapshot(pos, 1, 24f, 0f, out byte p, out _, out byte last));
            Assert.Equal(80, p);
            Assert.Equal(80, last);
        }
    }

    public class FootTrafficFitnessTests
    {
        [Theory]
        [InlineData(0f, 0.2f, 1f)]
        [InlineData(1f, 0.2f, 0.2f)]
        [InlineData(0.5f, 0.2f, 0.6f)]
        [InlineData(0.5f, 0f, 0.5f)]
        public void TrafficMultiplier_lerps_to_floor(float pressure, float floor, float expected)
        {
            float mult = EcologySpreadFitness.TrafficMultiplierFor(pressure, floor);
            Assert.Equal(expected, mult, 3);
        }

        [Fact]
        public void Trampled_event_still_resolves_impact_for_Apply_gate()
        {
            Assert.True(WildSpeciesSoilSuccession.TryGetImpact("anything", SoilSuccessionEvent.Trampled, out _));
        }

        [Theory]
        [InlineData(20, 8, 80)]
        [InlineData(10, 8, 40)]
        [InlineData(40, 8, 127)] // clamped
        public void EffectiveWearStep_from_steps(int steps, int perStep, int expected)
        {
            var cfg = new EcosystemConfig
            {
                FootTrafficStepsToFullCoverageWear = steps,
                FootTrafficPressurePerStep = perStep,
                FootTrafficSoilWearPressureStep = 99,
            };
            Assert.Equal(expected, FootTrafficWear.EffectiveWearStep(cfg));
        }

        [Fact]
        public void EffectiveWearStep_uses_override_when_steps_zero()
        {
            var cfg = new EcosystemConfig
            {
                FootTrafficStepsToFullCoverageWear = 0,
                FootTrafficPressurePerStep = 8,
                FootTrafficSoilWearPressureStep = 55,
            };
            Assert.Equal(55, FootTrafficWear.EffectiveWearStep(cfg));
        }

        [Theory]
        [InlineData(0, 80, 0)]
        [InlineData(79, 80, 0)]
        [InlineData(80, 80, 1)]
        [InlineData(160, 80, 2)]
        [InlineData(255, 80, 2)]
        public void TargetWearIndex_caps_at_verysparse(byte pressure, byte wearStep, int expected)
        {
            Assert.Equal(expected, FootTrafficWear.TargetWearIndex(pressure, wearStep));
        }
    }

    public class SoilTrafficCoverageTests
    {
        [Theory]
        [InlineData("soil-high-normal", "high", "normal")]
        [InlineData("soil-verylow-sparse", "verylow", "sparse")]
        [InlineData("soil-medium-verysparse", "medium", "verysparse")]
        [InlineData("soil-compost-none", "compost", "none")]
        public void TrySplitSoilPath_keeps_fertility_segment(string path, string fert, string coverage)
        {
            Assert.True(SoilTrafficCoverage.TrySplitSoilPath(path, out string f, out string c));
            Assert.Equal(fert, f);
            Assert.Equal(coverage, c);
        }

        [Theory]
        [InlineData("farmland-high-dry")]
        [InlineData("soil-high")]
        [InlineData("soil-")]
        [InlineData("dirt")]
        public void TrySplitSoilPath_rejects_non_coverage_codes(string path)
        {
            Assert.False(SoilTrafficCoverage.TrySplitSoilPath(path, out _, out _));
        }

        [Theory]
        [InlineData("normal", "sparse")]
        [InlineData("sparse", "verysparse")]
        [InlineData("verysparse", null)]
        [InlineData("none", null)]
        public void StepCoverageDown_stops_at_verysparse(string from, string expected)
        {
            Assert.Equal(expected, SoilTrafficCoverage.StepCoverageDown(from));
        }

        [Theory]
        [InlineData("none", "verysparse")]
        [InlineData("verysparse", "sparse")]
        [InlineData("sparse", "normal")]
        [InlineData("normal", null)]
        public void StepCoverageUp_walks_chain(string from, string expected)
        {
            Assert.Equal(expected, SoilTrafficCoverage.StepCoverageUp(from));
        }
    }
}
