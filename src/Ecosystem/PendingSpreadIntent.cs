using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Evaluated spread winner awaiting SetBlock commit (Phase 6.5).</summary>
    internal sealed class PendingSpreadIntent
    {
        public BlockPos ParentOrigin { get; set; }
        public BlockPos TargetPos { get; set; }
        public Block SpreadBlock { get; set; }
        public PlantRequirements Requirements { get; set; }
        public bool Displacing { get; set; }

        public Vec2i TargetChunk => ReproducerRegistry.ToChunkCoord(TargetPos);
    }
}
