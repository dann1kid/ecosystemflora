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

        public bool IsMatureBlock(Block block)
        {
            if (block?.Code == null) return false;
            string path = block.Code.Path;
            if (path != null && path.Contains("-harvested-")) return false;
            if (block.Code.Equals(MatureBlockCode)) return true;
            return PlantCodeHelper.SameEcologySpecies(block.Code, MatureBlockCode);
        }
    }
}
