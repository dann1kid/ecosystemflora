using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    /// <summary>Floating pad mat spread for water-surface plants (water lily).</summary>
    internal static class SurfaceMatSpread
    {
        internal const int DefaultVerticalReach = 1;

        static readonly MatEdgeTopology Topology = new MatEdgeTopology(
            MatConnectivity.Chebyshev8,
            IsSameSurfaceSpecies);

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || req.Habitat != EcologyHabitat.WaterSurface || req.SuppressSurfaceMatSpread) return;

            if (req.SpreadMode != SpreadMode.SurfaceMat && !EcosystemConfig.Loaded.UseSurfaceMatSpreadForLilies) return;

            req.SpreadMode = SpreadMode.SurfaceMat;
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
            if (req == null || !req.UsesSurfaceMatSpread || rand == null) return MatSpreadCollectMode.NotApplicable;

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
            if (radius <= 0
                && SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row)
                && row.SeedDispersalRadius > 0)
            {
                radius = row.SeedDispersalRadius;
            }
            else if (radius <= 0
                && SpeciesEcologyLegacyAccess.TryGetAquaticSeedDispersal(req.Species, out _, out int legacyRadius)
                && legacyRadius > 0)
            {
                radius = legacyRadius;
            }

            if (radius <= 0) radius = 4;

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

            if (chance <= 0f) return 0f;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            float scale = cfg != null ? cfg.RhizomeSeedDispersalChanceScale : 1f;
            if (scale <= 0f) return 0f;

            return System.Math.Min(1f, chance * scale);
        }

        /// <summary>Chebyshev distance 1 — pad touches including diagonals.</summary>
        public static bool IsMatStep(int dx, int dz)
        {
            return Topology.IsStep(dx, dz);
        }

        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species, int verticalReach = DefaultVerticalReach)
        {
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach, Topology);
        }

        static bool IsSameSurfaceSpecies(Block block, string species)
        {
            if (block == null || block.Id == 0 || string.IsNullOrEmpty(species)) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) != species) return false;
            return PlantCodeHelper.GetEcologyHabitat(block) == EcologyHabitat.WaterSurface;
        }
    }
}
