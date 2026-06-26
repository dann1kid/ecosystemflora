using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Rhizome-style spread for reed mats: frontier plants, orthogonal step only.</summary>
    internal static class RhizomeSpread
    {
        internal const int DefaultVerticalReach = 2;
        internal const float DefaultSeedDispersalChance = 0.08f;
        internal const int DefaultSeedDispersalRadius = 5;

        static readonly MatEdgeTopology Topology = new MatEdgeTopology(
            MatConnectivity.Orthogonal4,
            (block, species) => PlantCodeHelper.IsReedBlock(block)
                && PlantCodeHelper.ResolveEcologySpecies(block) == species);

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || req.Habitat != EcologyHabitat.ReedNearWater || req.SuppressRhizomeSpread) return;

            if (req.SpreadMode != SpreadMode.RhizomeMat && !EcosystemConfig.Loaded.UseRhizomeSpreadForReeds) return;

            req.SpreadMode = SpreadMode.RhizomeMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;

            if (WildAquaticEcology.TryGet(req.Species, out WildAquaticEcology.Profile aquatic))
            {
                if (req.SeedDispersalChance <= 0f) req.SeedDispersalChance = aquatic.SeedDispersalChance;
                if (req.SeedDispersalRadius <= 0) req.SeedDispersalRadius = aquatic.SeedDispersalRadius;
            }
        }

        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand)
        {
            if (req == null || !req.UsesRhizomeSpread || rand == null) return MatSpreadCollectMode.NotApplicable;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.RhizomeSeedDispersalEnabled) return MatSpreadCollectMode.MatEdge;

            return rand.NextDouble() < EffectiveSeedDispersalChance(req)
                ? MatSpreadCollectMode.SeedDispersal
                : MatSpreadCollectMode.MatEdge;
        }

        public static int ResolveSearchRadius(PlantRequirements req, MatSpreadCollectMode mode, int defaultRadius)
        {
            if (req == null || mode == MatSpreadCollectMode.NotApplicable) return defaultRadius;

            if (mode == MatSpreadCollectMode.MatEdge)
            {
                return req.SpreadRadius > 0 ? req.SpreadRadius : 1;
            }

            int radius = req.SeedDispersalRadius;
            if (radius <= 0)
            {
                if (WildAquaticEcology.TryGet(req.Species, out WildAquaticEcology.Profile aquatic)
                    && aquatic.SeedDispersalRadius > 0)
                {
                    radius = aquatic.SeedDispersalRadius;
                }
                else
                {
                    radius = DefaultSeedDispersalRadius;
                }
            }

            return radius;
        }

        public static float EffectiveSeedDispersalChance(PlantRequirements req)
        {
            if (req == null) return 0f;

            float chance = req.SeedDispersalChance;
            if (chance <= 0f
                && WildAquaticEcology.TryGet(req.Species, out WildAquaticEcology.Profile aquatic)
                && aquatic.SeedDispersalChance > 0f)
            {
                chance = aquatic.SeedDispersalChance;
            }

            if (chance <= 0f) chance = DefaultSeedDispersalChance;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            float scale = cfg != null ? cfg.RhizomeSeedDispersalChanceScale : 1f;
            if (scale <= 0f) return 0f;

            return System.Math.Min(1f, chance * scale);
        }

        public static bool IsOrthogonalStep(int dx, int dz)
        {
            return Topology.IsStep(dx, dz);
        }

        /// <summary>True when at least one horizontal neighbor column lacks same-species reed.</summary>
        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species, int verticalReach = DefaultVerticalReach)
        {
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach, Topology);
        }
    }
}
