using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
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

        [Fact]
        public void ResolveFallenLogBlock_PrefersRottenDebarkedHorizontal()
        {
            var rottenNs = new Block { BlockId = 10, Code = new AssetLocation("game:debarkedlog-rotten-ns") };
            var rottenWe = new Block { BlockId = 11, Code = new AssetLocation("game:debarkedlog-rotten-we") };
            var freshOak = new Block { BlockId = 12, Code = new AssetLocation("game:debarkedlog-oak-ns") };
            var acc = new EcologyTestBlockAccessor(new Block[] { new Block { BlockId = 0 }, rottenNs, rottenWe, freshOak });

            Block first = TreeDecayRemains.ResolveFallenLogBlock(acc, "oak", 0);
            Block second = TreeDecayRemains.ResolveFallenLogBlock(acc, "oak", 1);

            Assert.Same(rottenNs, first);
            Assert.Same(rottenWe, second);
        }

        [Fact]
        public void ResolveFallenLogBlock_FallsBackToSpeciesDebarkedWhenRottenMissing()
        {
            var freshOakNs = new Block { BlockId = 10, Code = new AssetLocation("game:debarkedlog-oak-ns") };
            var acc = new EcologyTestBlockAccessor(new Block[] { new Block { BlockId = 0 }, freshOakNs });

            Block block = TreeDecayRemains.ResolveFallenLogBlock(acc, "oak", 0);

            Assert.Same(freshOakNs, block);
        }
    }
}
