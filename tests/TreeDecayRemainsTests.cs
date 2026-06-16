using System.Collections.Generic;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class TreeDecayRemainsTests
    {
        [Fact]
        public void BuildScatterCandidates_Ring1And2_Count24()
        {
            var trunkBase = new BlockPos(100, 64, -50, 0);
            var list = new List<BlockPos>();

            TreeDecayRemains.BuildScatterCandidates(trunkBase, list);

            Assert.Equal(24, list.Count);
            Assert.DoesNotContain(trunkBase, list);
            Assert.All(list, p => Assert.Equal(trunkBase.Y, p.Y));
        }

        [Fact]
        public void ShuffleCandidates_IsDeterministic()
        {
            var trunkBase = new BlockPos(10, 70, 10, 0);
            var a = new List<BlockPos>();
            var b = new List<BlockPos>();

            TreeDecayRemains.BuildScatterCandidates(trunkBase, a);
            TreeDecayRemains.BuildScatterCandidates(trunkBase, b);

            TreeDecayRemains.ShuffleCandidates(trunkBase, "oak", a);
            TreeDecayRemains.ShuffleCandidates(trunkBase, "oak", b);

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i], b[i]);
            }
        }

        [Fact]
        public void ShuffleCandidates_DiffersByWood()
        {
            var trunkBase = new BlockPos(10, 70, 10, 0);
            var oak = new List<BlockPos>();
            var birch = new List<BlockPos>();

            TreeDecayRemains.BuildScatterCandidates(trunkBase, oak);
            TreeDecayRemains.BuildScatterCandidates(trunkBase, birch);

            TreeDecayRemains.ShuffleCandidates(trunkBase, "oak", oak);
            TreeDecayRemains.ShuffleCandidates(trunkBase, "birch", birch);

            Assert.NotEqual(oak[0], birch[0]);
        }
    }
}
