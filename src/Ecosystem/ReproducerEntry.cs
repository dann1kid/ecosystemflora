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

        public double EstablishedAtHours { get; set; }

        public int FailedSurvivalChecks { get; set; }

        public double NextStressCheckAt { get; set; }

        /// <summary>Cumulative ticks spent near a player; resets when no player is close.</summary>
        public int TramplingExposure { get; set; }

        /// <summary>Calendar years since ecology registration (senescence axis; independent of structure).</summary>
        public int TreeAgeYears { get; set; }

        /// <summary>Last in-game year when calendar age advanced and maturation ran.</summary>
        public int LastTreeGrowthYear { get; set; } = int.MinValue;

        internal int EntriesIndex { get; set; } = -1;

        internal int ChunkListIndex { get; set; } = -1;

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
            if (Requirements?.Habitat == EcologyHabitat.MyceliumAnchor)
            {
                return false;
            }

            if (block?.Code == null) return false;
            string path = block.Code.Path;
            if (path != null && path.Contains("-harvested-")) return false;
            if (block.Code.Equals(MatureBlockCode)) return true;
            return PlantCodeHelper.SameEcologySpecies(block.Code, MatureBlockCode);
        }
    }
}
