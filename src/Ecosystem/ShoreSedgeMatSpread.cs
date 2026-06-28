using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wet-shore sedges: vegetative mat-edge steps plus optional seed dispersal jumps.
    /// </summary>
    internal static class ShoreSedgeMatSpread
    {
        static readonly MatEdgeTopology Topology = new MatEdgeTopology(
            MatConnectivity.Orthogonal4,
            IsShoreSedgeBlock);

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || !IsShoreSedgeSpecies(req.Species)) return;
            if (!EcosystemConfig.Loaded.EnableShoreSedgeMatSpread) return;

            if (SpeciesEcologyRegistry.IsLoaded)
            {
                req.SpreadMode = SpreadMode.ShoreSedgeMat;
                if (req.SpreadRadius <= 0) req.SpreadRadius = 1;
                return;
            }

            SpeciesEcologyLegacyAccess.ApplyShoreSedgeMatSpreadLegacy(req);
        }

        static bool IsShoreSedgeSpecies(string species)
        {
            if (SpeciesEcologyRegistry.IsLoaded && SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow row))
            {
                return row.Taxon == "shore_sedge";
            }

            return EcologyShoreSedgeSpecies.IsKnown(species);
        }

        static bool IsShoreSedgeBlock(Block block, string species)
        {
            if (block == null || block.Id == 0 || string.IsNullOrEmpty(species)) return false;
            if (ShoreSedgeJuvenileBlocks.IsJuvenileBlock(block)
                && string.Equals(ShoreSedgeJuvenileBlocks.SpeciesFromJuvenile(block), species, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return block.Code?.Path != null
                && block.Code.Path.StartsWith("tallplant-brownsedge")
                && string.Equals(PlantCodeHelper.ResolveEcologySpecies(block), species, System.StringComparison.OrdinalIgnoreCase);
        }

        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand)
        {
            if (req == null || !req.UsesShoreSedgeMatSpread || rand == null) return MatSpreadCollectMode.NotApplicable;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.RhizomeSeedDispersalEnabled) return MatSpreadCollectMode.MatEdge;

            float seedChance = EffectiveSeedDispersalChance(req);
            if (seedChance <= 0f) return MatSpreadCollectMode.MatEdge;

            return rand.NextDouble() < seedChance
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
                && SpeciesEcologyLegacyAccess.TryGetShoreSedgeSeedDispersal(req.Species, out _, out int legacyRadius)
                && legacyRadius > 0)
            {
                radius = legacyRadius;
            }

            return radius > 0 ? radius : 4;
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
                && SpeciesEcologyLegacyAccess.TryGetShoreSedgeSeedDispersal(req.Species, out float legacyChance, out _)
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

        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species)
        {
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach: 0, Topology);
        }

        public static bool IsOrthogonalStep(int dx, int dz)
        {
            return Topology.IsStep(dx, dz);
        }
    }
}
