using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Terrestrial berry colonies: mat-edge rhizome/runner steps plus optional seed/animal dispersal jumps.
    /// </summary>
    internal static class BerryColonySpread
    {
        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || !WildBerryEcology.TryGet(req.Species, out WildBerryEcology.Profile profile)) return;
            if (!EcosystemConfig.Loaded.EnableBerryColonySpread)
            {
                if (profile.SpreadMode == SpreadMode.BerryColonyMat)
                {
                    req.SpreadMode = SpreadMode.Independent;
                }

                if (profile.IndependentSpreadRadius > 0)
                {
                    req.SpreadRadius = profile.IndependentSpreadRadius;
                }

                return;
            }

            if (profile.SpreadMode == SpreadMode.BerryColonyMat)
            {
                req.SpreadMode = SpreadMode.BerryColonyMat;
                req.SpreadRadius = profile.MatSpreadRadius > 0 ? profile.MatSpreadRadius : 1;
                if (profile.SeedDispersalChance > 0f) req.SeedDispersalChance = profile.SeedDispersalChance;
                if (profile.SeedDispersalRadius > 0) req.SeedDispersalRadius = profile.SeedDispersalRadius;
                return;
            }

            if (profile.IndependentSpreadRadius > 0)
            {
                req.SpreadRadius = profile.IndependentSpreadRadius;
            }
        }

        static MatEdgeTopology TopologyFor(string species)
        {
            if (!WildBerryEcology.TryGet(species, out WildBerryEcology.Profile profile))
            {
                return new MatEdgeTopology(
                    MatConnectivity.Orthogonal4,
                    IsBerrySpecies);
            }

            return new MatEdgeTopology(profile.MatConnectivity, IsBerrySpecies);
        }

        static bool IsBerrySpecies(Block block, string species)
        {
            return block != null && block.Id != 0
                && PlantCodeHelper.IsWildBerryBushBlock(block)
                && string.Equals(PlantCodeHelper.ResolveEcologySpecies(block), species, System.StringComparison.OrdinalIgnoreCase);
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
                && WildBerryEcology.TryGet(req.Species, out WildBerryEcology.Profile profile)
                && profile.SeedDispersalRadius > 0)
            {
                radius = profile.SeedDispersalRadius;
            }

            return radius > 0 ? radius : 5;
        }

        public static float EffectiveSeedDispersalChance(PlantRequirements req)
        {
            if (req == null) return 0f;

            float chance = req.SeedDispersalChance;
            if (chance <= 0f
                && WildBerryEcology.TryGet(req.Species, out WildBerryEcology.Profile profile)
                && profile.SeedDispersalChance > 0f)
            {
                chance = profile.SeedDispersalChance;
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
