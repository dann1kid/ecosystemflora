using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeCalendarAgeStoreTests
    {
        static ReproducerEntry MakeTreeEntry(BlockPos pos, int ageYears, int lastYear)
        {
            return new ReproducerEntry(
                pos,
                new AssetLocation("game:sapling-oak-free"),
                new AssetLocation("game:log-grown-oak-ud"),
                new PlantRequirements { Species = "oak", Habitat = EcologyHabitat.TerrestrialTree },
                0)
            {
                TreeAgeYears = ageYears,
                LastTreeGrowthYear = lastYear,
            };
        }

        [Fact]
        public void SerializeRoundTrip_PreservesAge()
        {
            var store = new TreeCalendarAgeStore();
            var pos = new BlockPos(100, 64, -200, 0);
            var entry = MakeTreeEntry(pos, 17, 42);

            store.Capture(entry, "oak");
            byte[] bytes = store.SerializeForTests();

            var loaded = new TreeCalendarAgeStore();
            loaded.LoadFromBytes(bytes);

            Assert.Equal(1, loaded.RecordCount);
            Assert.True(loaded.TryGetRecord(pos, out TreeCalendarAgeRecord rec));
            Assert.Equal(17, rec.AgeYears);
            Assert.Equal(42, rec.LastGrowthYear);
            Assert.Equal("oak", rec.Wood);
        }

        [Fact]
        public void TryRestore_MatchingWood_AppliesAge()
        {
            var store = new TreeCalendarAgeStore();
            var pos = new BlockPos(10, 70, 10, 0);
            var entry = MakeTreeEntry(pos, 0, 0);

            store.Capture(MakeTreeEntry(pos, 55, 9), "pine");

            Assert.True(store.TryRestore(entry, pos, "pine"));
            Assert.Equal(55, entry.TreeAgeYears);
            Assert.Equal(9, entry.LastTreeGrowthYear);
        }

        [Fact]
        public void TryRestore_WoodMismatch_DoesNotApply()
        {
            var store = new TreeCalendarAgeStore();
            var pos = new BlockPos(10, 70, 10, 0);
            store.Capture(MakeTreeEntry(pos, 40, 5), "oak");

            var entry = new ReproducerEntry(
                pos,
                new AssetLocation("game:sapling-birch-free"),
                new AssetLocation("game:log-grown-birch-ud"),
                new PlantRequirements { Species = "birch", Habitat = EcologyHabitat.TerrestrialTree },
                0);

            Assert.False(store.TryRestore(entry, pos, "birch"));
            Assert.Equal(0, entry.TreeAgeYears);
        }
    }
}
