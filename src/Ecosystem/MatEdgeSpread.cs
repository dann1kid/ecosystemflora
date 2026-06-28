using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Neighbor connectivity for a mat-edge patch.</summary>
    internal enum MatConnectivity
    {
        /// <summary>Four cardinal neighbors; step is Manhattan distance 1.</summary>
        Orthogonal4,

        /// <summary>Eight neighbors incl. diagonals; step is Chebyshev distance 1.</summary>
        Chebyshev8,
    }

    /// <summary>
    /// Describes one mat-edge spread topology: how neighbors connect and what counts as the same
    /// patch member. Reed rhizome, fern rhizome, and floating surface mats differ only in these two
    /// dimensions; the frontier/column scan itself is shared (see <see cref="MatEdgeSpread"/>).
    /// </summary>
    internal sealed class MatEdgeTopology
    {
        public readonly MatConnectivity Connectivity;
        public readonly System.Func<Block, string, bool> Matches;
        readonly int[][] dirs;

        public MatEdgeTopology(MatConnectivity connectivity, System.Func<Block, string, bool> matches)
        {
            Connectivity = connectivity;
            Matches = matches;
            dirs = connectivity == MatConnectivity.Chebyshev8 ? Neighbor8 : Orthogonal4;
        }

        public int[][] Directions => dirs;

        public bool IsStep(int dx, int dz)
        {
            return Connectivity == MatConnectivity.Chebyshev8
                ? System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dz)) == 1
                : System.Math.Abs(dx) + System.Math.Abs(dz) == 1;
        }

        static readonly int[][] Orthogonal4 =
        {
            new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 },
        };

        static readonly int[][] Neighbor8 =
        {
            new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 },
            new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 },
        };
    }

    /// <summary>Shared mat-edge frontier test: a cell is frontier when any neighbor column lacks a
    /// same-patch member, per the supplied topology and vertical reach.</summary>
    internal static class MatEdgeSpread
    {
        static readonly BlockPos NeighborScratch = new BlockPos(0);

        public static bool IsFrontier(
            IBlockAccessor acc,
            BlockPos origin,
            string species,
            int verticalReach,
            MatEdgeTopology topology)
        {
            if (acc == null || origin == null || string.IsNullOrEmpty(species) || topology == null) return true;
            if (verticalReach < 0) verticalReach = 0;

            NeighborScratch.dimension = origin.dimension;

            int[][] dirs = topology.Directions;
            for (int i = 0; i < dirs.Length; i++)
            {
                if (!NeighborColumnMatches(acc, origin, dirs[i][0], dirs[i][1], species, verticalReach, topology.Matches))
                {
                    return true;
                }
            }

            return false;
        }

        static bool NeighborColumnMatches(
            IBlockAccessor acc,
            BlockPos origin,
            int dx,
            int dz,
            string species,
            int verticalReach,
            System.Func<Block, string, bool> matches)
        {
            int nx = origin.X + dx;
            int nz = origin.Z + dz;

            for (int y = origin.Y - verticalReach; y <= origin.Y + verticalReach; y++)
            {
                NeighborScratch.Set(nx, y, nz);
                Block block = acc.GetBlock(NeighborScratch);
                if (matches(block, species)) return true;
            }

            return false;
        }
    }
}
