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

        static readonly int[][] OrthogonalDirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

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

        public static RhizomeCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand)
        {
            if (req == null || !req.UsesRhizomeSpread || rand == null) return RhizomeCollectMode.NotApplicable;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.RhizomeSeedDispersalEnabled) return RhizomeCollectMode.RhizomeEdge;

            return rand.NextDouble() < EffectiveSeedDispersalChance(req)
                ? RhizomeCollectMode.SeedDispersal
                : RhizomeCollectMode.RhizomeEdge;
        }

        public static int ResolveSearchRadius(PlantRequirements req, RhizomeCollectMode mode, int defaultRadius)
        {
            if (req == null || mode == RhizomeCollectMode.NotApplicable) return defaultRadius;

            if (mode == RhizomeCollectMode.RhizomeEdge)
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
            return System.Math.Abs(dx) + System.Math.Abs(dz) == 1;
        }

        /// <summary>True when at least one horizontal neighbor column lacks same-species reed.</summary>
        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species, int verticalReach = DefaultVerticalReach)
        {
            if (acc == null || origin == null || string.IsNullOrEmpty(species)) return true;

            if (verticalReach < 0) verticalReach = 0;

            for (int i = 0; i < OrthogonalDirs.Length; i++)
            {
                if (!NeighborColumnHasSameSpecies(acc, origin, OrthogonalDirs[i][0], OrthogonalDirs[i][1], species, verticalReach))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NeighborColumnHasSameSpecies(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            string species,
            int verticalReach)
        {
            int nx = origin.X + dx;
            int nz = origin.Z + dz;

            for (int y = origin.Y - verticalReach; y <= origin.Y + verticalReach; y++)
            {
                var checkPos = new BlockPos(nx, y, nz, origin.dimension);
                Block block = acc.GetBlock(checkPos);
                if (!PlantCodeHelper.IsReedBlock(block)) continue;
                if (PlantCodeHelper.ResolveEcologySpecies(block) == species) return true;
            }

            return false;
        }
    }
}
