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

        /// <summary>Earliest hour allowed after a successful offspring spawn (post-spawn cooldown).</summary>
        public double NextSpawnAllowedAtHours { get; set; }

        /// <summary>Bumped when ecology events occur nearby (Phase 6 wake).</summary>
        public ulong WakeGeneration { get; set; }

        /// <summary>Last wake generation consumed by a spread attempt.</summary>
        public ulong LastProcessedWakeGeneration { get; set; }

        public double EstablishedAtHours { get; set; }

        public int FailedSurvivalChecks { get; set; }

        public double NextStressCheckAt { get; set; }

        /// <summary>Cumulative ticks spent near a player; resets when no player is close.</summary>
        public int TramplingExposure { get; set; }

        /// <summary>Calendar years since ecology registration (senescence axis; independent of structure).</summary>
        public int TreeAgeYears { get; set; }

        /// <summary>Last in-game year when calendar age advanced and maturation ran.</summary>
        public int LastTreeGrowthYear { get; set; } = int.MinValue;

        /// <summary>Phased senescence after calendar lifespan (one stage per game year).</summary>
        public TreeSenescencePhase TreeSenescencePhase { get; set; }

        /// <summary>Meadow flower phenology phase when <see cref="FlowerPhenology"/> is enabled.</summary>
        public FlowerPhenologyPhase PhenologyPhase { get; set; }

        /// <summary>Energy toward bloom (0..threshold+); depletes during bloom.</summary>
        public float PhenologyEnergy { get; set; }

        /// <summary>Last game-hour when phenology was advanced for this entry.</summary>
        public double LastPhenologyUpdateHours { get; set; }

        internal int EntriesIndex { get; set; } = -1;

        internal int ChunkListIndex { get; set; } = -1;

        internal MatSpreadCollectMode LastSpreadCollectMode { get; set; }

        internal bool LastSpreadPlaced { get; set; }

        internal string LastSpreadFailureReason { get; set; }

        internal double LastSpreadAttemptAtHours { get; set; }

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

        /// <summary>Mature vanilla flower or phenology juvenile at the registered origin.</summary>
        public bool IsRegisteredPlantBlock(Block block) => FlowerPhenology.IsRegisteredPlantBlock(this, block);
    }
}
