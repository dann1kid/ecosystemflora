using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem.SpeciesEcology;

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
            if (req == null || string.IsNullOrEmpty(req.Species)) return;
            if (!IsFernEcologySpecies(req.Species)) return;
            if (!EcosystemConfig.Loaded.EnableFernRhizomeSpread) return;

            req.SpreadMode = SpreadMode.FernRhizomeMat;
            if (req.SpreadRadius <= 0) req.SpreadRadius = 1;
        }

        static bool IsFernEcologySpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;

            if (SpeciesEcologyRegistry.IsLoaded && SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow row))
            {
                return row.Taxon == "fern";
            }

            return EcologyFernSpecies.IsKnown(species);
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
