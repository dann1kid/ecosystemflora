using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Rhizome-style spread for reed mats: frontier plants, orthogonal step only.</summary>
    internal static class RhizomeSpread
    {
        internal const int DefaultVerticalReach = 2;

        static readonly int[][] OrthogonalDirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || req.Habitat != EcologyHabitat.ReedNearWater || req.SuppressRhizomeSpread) return;

            if (req.SpreadMode != SpreadMode.RhizomeMat && !EcosystemConfig.Loaded.UseRhizomeSpreadForReeds) return;

            req.SpreadMode = SpreadMode.RhizomeMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;
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
