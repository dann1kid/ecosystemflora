using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Floating pad mat spread for water-surface plants (water lily).</summary>
    internal static class SurfaceMatSpread
    {
        internal const int DefaultVerticalReach = 1;

        static readonly int[][] Neighbor8 =
        {
            new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 },
            new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 },
        };

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || req.Habitat != EcologyHabitat.WaterSurface || req.SuppressSurfaceMatSpread) return;

            if (req.SpreadMode != SpreadMode.SurfaceMat && !EcosystemConfig.Loaded.UseSurfaceMatSpreadForLilies) return;

            req.SpreadMode = SpreadMode.SurfaceMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;

            if (WildAquaticEcology.TryGet(req.Species, out WildAquaticEcology.Profile aquatic))
            {
                if (req.SeedDispersalChance <= 0f) req.SeedDispersalChance = aquatic.SeedDispersalChance;
                if (req.SeedDispersalRadius <= 0) req.SeedDispersalRadius = aquatic.SeedDispersalRadius;
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
                && WildAquaticEcology.TryGet(req.Species, out WildAquaticEcology.Profile aquatic)
                && aquatic.SeedDispersalRadius > 0)
            {
                radius = aquatic.SeedDispersalRadius;
            }

            if (radius <= 0) radius = 4;

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

            if (chance <= 0f) return 0f;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            float scale = cfg != null ? cfg.RhizomeSeedDispersalChanceScale : 1f;
            if (scale <= 0f) return 0f;

            return System.Math.Min(1f, chance * scale);
        }

        /// <summary>Chebyshev distance 1 — pad touches including diagonals.</summary>
        public static bool IsMatStep(int dx, int dz)
        {
            return System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dz)) == 1;
        }

        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species, int verticalReach = DefaultVerticalReach)
        {
            if (acc == null || origin == null || string.IsNullOrEmpty(species)) return true;

            if (verticalReach < 0) verticalReach = 0;

            for (int i = 0; i < Neighbor8.Length; i++)
            {
                if (!NeighborHasSameSpecies(acc, origin, Neighbor8[i][0], Neighbor8[i][1], species, verticalReach))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NeighborHasSameSpecies(
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
                if (!IsSameSurfaceSpecies(block, species)) continue;
                return true;
            }

            return false;
        }

        static bool IsSameSurfaceSpecies(Block block, string species)
        {
            if (block == null || block.Id == 0 || string.IsNullOrEmpty(species)) return false;
            if (PlantCodeHelper.ResolveEcologySpecies(block) != species) return false;
            return PlantCodeHelper.GetEcologyHabitat(block) == EcologyHabitat.WaterSurface;
        }
    }
}
