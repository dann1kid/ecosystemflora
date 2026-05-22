using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public sealed class ReproducerEntry : IReproducible
    {
        public BlockPos Origin { get; }
        public AssetLocation JuvenileBlockCode { get; }
        public AssetLocation MatureBlockCode { get; }
        public PlantRequirements Requirements { get; }
        public double NextAttemptHours { get; set; }

        public ReproducerEntry(
            BlockPos origin,
            AssetLocation juvenileBlockCode,
            AssetLocation matureBlockCode,
            PlantRequirements requirements,
            double nextAttemptHours)
        {
            Origin = origin;
            JuvenileBlockCode = juvenileBlockCode;
            MatureBlockCode = matureBlockCode;
            Requirements = requirements;
            NextAttemptHours = nextAttemptHours;
        }

        public bool IsMatureBlock(Block block) => block?.Code != null && block.Code.Equals(MatureBlockCode);
    }
}
