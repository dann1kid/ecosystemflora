using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Terrestrial berry colonies: mat-edge rhizome/runner steps plus optional seed/animal dispersal jumps.
    /// </summary>
    internal static class BerryColonySpread
    {
        static readonly MatEdgeTopology OrthogonalTopology = new MatEdgeTopology(
            MatConnectivity.Orthogonal4,
            IsBerrySpeciesBlock);

        static readonly MatEdgeTopology ChebyshevTopology = new MatEdgeTopology(
            MatConnectivity.Chebyshev8,
            IsBerrySpeciesBlock);

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || string.IsNullOrEmpty(req.Species)) return;

            if (SpeciesEcologyRegistry.IsLoaded
                && SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row))
            {
                if (row.Taxon != "berry") return;
                ApplyPolicyGates(req);
                return;
            }

            SpeciesEcologyLegacyAccess.ApplyBerryColonySpreadLegacy(req);
        }

        static void ApplyPolicyGates(PlantRequirements req)
        {
            if (!EcosystemConfig.Loaded.EnableBerryColonySpread)
            {
                if (req.SpreadMode == SpreadMode.BerryColonyMat)
                {
                    req.SpreadMode = SpreadMode.Independent;
                    if (SpeciesEcologyRegistry.TryGet(req.Species, out SpeciesEcologyCsvRow row)
                        && row.IndependentSpreadRadius > 0)
                    {
                        req.SpreadRadius = row.IndependentSpreadRadius;
                    }
                }

                return;
            }

            if (req.UsesBerryColonySpread && req.SpreadRadius <= 0)
            {
                req.SpreadRadius = 1;
            }
        }

        static bool IsBerrySpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;

            if (SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow row))
            {
                return row.Taxon == "berry";
            }

            return EcologyBerrySpecies.IsKnown(species);
        }

        static MatEdgeTopology TopologyFor(string species)
        {
            return ResolveConnectivity(species) == MatConnectivity.Chebyshev8
                ? ChebyshevTopology
                : OrthogonalTopology;
        }

        static MatConnectivity ResolveConnectivity(string species)
        {
            if (string.IsNullOrEmpty(species)) return MatConnectivity.Orthogonal4;

            if (SpeciesEcologyRegistry.TryGetMatConnectivity(species, out MatConnectivity csvConnectivity))
            {
                return csvConnectivity;
            }

            if (SpeciesEcologyLegacyAccess.TryGetBerryMatConnectivity(species, out MatConnectivity legacyConnectivity))
            {
                return legacyConnectivity;
            }

            return MatConnectivity.Orthogonal4;
        }

        static bool IsBerrySpeciesBlock(Block block, string species)
        {
            if (block == null || (block.Id == 0 && block.BlockId == 0) || string.IsNullOrEmpty(species)) return false;

            if (PlantCodeHelper.IsThirdPartyEcologyBlock(block))
            {
                return EcologyBerrySpecies.IsKnown(species)
                    && string.Equals(PlantCodeHelper.ResolveEcologySpecies(block), species, System.StringComparison.OrdinalIgnoreCase);
            }

            return PlantCodeHelper.IsWildBerryBushBlock(block)
                && string.Equals(PlantCodeHelper.GetEcologySpecies(block.Code), species, System.StringComparison.OrdinalIgnoreCase);
        }

        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand)
        {
            if (req == null || !req.UsesBerryColonySpread || rand == null) return MatSpreadCollectMode.NotApplicable;

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
                && SpeciesEcologyLegacyAccess.TryGetBerrySeedDispersal(req.Species, out _, out int legacyRadius)
                && legacyRadius > 0)
            {
                radius = legacyRadius;
            }

            return radius > 0 ? radius : 5;
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
                && SpeciesEcologyLegacyAccess.TryGetBerrySeedDispersal(req.Species, out float legacyChance, out _)
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
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach: 0, TopologyFor(species));
        }

        public static bool IsStep(int dx, int dz, string species)
        {
            return TopologyFor(species).IsStep(dx, dz);
        }
    }
}

