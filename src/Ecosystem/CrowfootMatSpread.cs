using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Rhizome-style mat edge spread for water-crowfoot columns (not reed topology).</summary>
    internal static class CrowfootMatSpread
    {
        internal const int DefaultVerticalReach = 3;

        static readonly MatEdgeTopology Topology = new MatEdgeTopology(
            MatConnectivity.Orthogonal4,
            (block, species) => PlantCodeHelper.IsWatercrowfoot(block?.Code));

        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand) =>
            MatSpreadCollectMode.MatEdge;

        public static int ResolveSearchRadius(PlantRequirements req, MatSpreadCollectMode mode, int defaultRadius)
        {
            if (req == null || mode == MatSpreadCollectMode.NotApplicable) return defaultRadius;
            return req.SpreadRadius > 0 ? req.SpreadRadius : 1;
        }

        public static bool IsOrthogonalStep(int dx, int dz) =>
            System.Math.Abs(dx) + System.Math.Abs(dz) == 1;

        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, string species, int verticalReach = DefaultVerticalReach)
        {
            return MatEdgeSpread.IsFrontier(acc, origin, species, verticalReach, Topology);
        }
    }
}
