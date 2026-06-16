using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using Xunit;

namespace WildFarming.Tests
{
    public class PendingSpreadQueueTests
    {
        [Fact]
        public void TargetChunk_MatchesReproducerRegistryChunkCoord()
        {
            var intent = new PendingSpreadIntent
            {
                TargetPos = new BlockPos(40, 64, 10),
            };

            Vec2i chunk = intent.TargetChunk;
            Vec2i expected = ReproducerRegistry.ToChunkCoord(intent.TargetPos);

            Assert.Equal(expected.X, chunk.X);
            Assert.Equal(expected.Y, chunk.Y);
        }

        [Fact]
        public void EnableTwoPhaseSpreadPlacement_DefaultsTrue()
        {
            Assert.True(new EcosystemConfig().EnableTwoPhaseSpreadPlacement);
        }
    }
}
