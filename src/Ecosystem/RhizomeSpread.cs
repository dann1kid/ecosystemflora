using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

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

            if (req.SeedDispersalChance <= 0f
                && SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row)
                && row.SeedDispersalChance > 0f)
            {
                req.SeedDispersalChance = row.SeedDispersalChance;
            }
            else if (req.SeedDispersalChance <= 0f
                && SpeciesEcologyLegacyAccess.TryGetAquaticSeedDispersal(req.Species, out float chance, out _)
                && chance > 0f)
            {
                req.SeedDispersalChance = chance;
            }

            if (req.SeedDispersalRadius <= 0
                && SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow rowRadius)
                && rowRadius.SeedDispersalRadius > 0)
            {
                req.SeedDispersalRadius = rowRadius.SeedDispersalRadius;
            }
            else if (req.SeedDispersalRadius <= 0
                && SpeciesEcologyLegacyAccess.TryGetAquaticSeedDispersal(req.Species, out _, out int radius)
                && radius > 0)
            {
                req.SeedDispersalRadius = radius;
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
                if (SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row)
                    && row.SeedDispersalRadius > 0)
                {
                    radius = row.SeedDispersalRadius;
                }
                else if (SpeciesEcologyLegacyAccess.TryGetAquaticSeedDispersal(req.Species, out _, out int legacyRadius)
                    && legacyRadius > 0)
                {
                    radius = legacyRadius;
                }
                else if (req.Habitat == EcologyHabitat.ReedNearWater)
                {
                    radius = DefaultSeedDispersalRadius;
                }
                else
                {
                    radius = defaultRadius;
                }
            }

            return radius;
        }

        public static float EffectiveSeedDispersalChance(PlantRequirements req)
        {
            if (req == null) return 0f;

            float chance = req.SeedDispersalChance;
            if (chance <= 0f
                && SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row)
                && row.SeedDispersalChance > 0f)
            {
                chance = row.SeedDispersalChance;
            }
            else if (chance <= 0f
                && SpeciesEcologyLegacyAccess.TryGetAquaticSeedDispersal(req.Species, out float legacyChance, out _)
                && legacyChance > 0f)
            {
                chance = legacyChance;
            }

            if (chance <= 0f && req.Habitat == EcologyHabitat.ReedNearWater)
            {
                chance = DefaultSeedDispersalChance;
            }

            if (chance <= 0f) return 0f;

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

