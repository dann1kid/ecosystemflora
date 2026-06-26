using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Terrestrial fern patches spread one orthogonal step from the mat edge (underground rhizome).</summary>
    internal static class FernRhizomeSpread
    {
        static readonly int[][] OrthogonalDirs = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || !EcologyFernSpecies.IsKnown(req.Species)) return;
            if (!EcosystemConfig.Loaded.EnableFernRhizomeSpread) return;

            req.SpreadMode = SpreadMode.FernRhizomeMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;
        }

        public static bool IsOrthogonalStep(int dx, int dz)
        {
            return System.Math.Abs(dx) + System.Math.Abs(dz) == 1;
        }

        /// <summary>True when a horizontal neighbor lacks the same fern species.</summary>
        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species)
        {
            if (acc == null || origin == null || string.IsNullOrEmpty(species)) return true;

            for (int i = 0; i < OrthogonalDirs.Length; i++)
            {
                if (!NeighborHasSameSpecies(acc, origin, OrthogonalDirs[i][0], OrthogonalDirs[i][1], species))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NeighborHasSameSpecies(IBlockAccessor acc, BlockPos origin, int dx, int dz, string species)
        {
            var checkPos = new BlockPos(origin.X + dx, origin.Y, origin.Z + dz, origin.dimension);
            Block block = acc.GetBlock(checkPos);
            if (block == null || block.Id == 0) return false;

            string neighborSpecies = PlantCodeHelper.ResolveEcologySpecies(block);
            return neighborSpecies != null
                && string.Equals(neighborSpecies, species, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
