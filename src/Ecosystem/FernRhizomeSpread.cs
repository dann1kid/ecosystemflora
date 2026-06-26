using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Terrestrial fern patches spread one orthogonal step from the mat edge (underground rhizome).</summary>
    internal static class FernRhizomeSpread
    {
        static readonly MatEdgeTopology Topology = new MatEdgeTopology(
            MatConnectivity.Orthogonal4,
            (block, species) => block != null && block.Id != 0
                && string.Equals(PlantCodeHelper.ResolveEcologySpecies(block), species, System.StringComparison.OrdinalIgnoreCase));

        public static void ApplyTo(PlantRequirements req)
        {
            if (req == null || !EcologyFernSpecies.IsKnown(req.Species)) return;
            if (!EcosystemConfig.Loaded.EnableFernRhizomeSpread) return;

            req.SpreadMode = SpreadMode.FernRhizomeMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;
        }

        public static bool IsOrthogonalStep(int dx, int dz)
        {
            return Topology.IsStep(dx, dz);
        }

        /// <summary>True when a horizontal neighbor lacks the same fern species (single-Y patch).</summary>
        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species)
        {
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach: 0, Topology);
        }
    }
}
