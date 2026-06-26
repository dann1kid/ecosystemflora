using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Single entry point that maps a plant's active mat-spread mode to its frontier test, step
    /// filter, and collect mode. Replaces the per-mode branch ladders that were duplicated across
    /// candidate collection (placement + solve batch), inspect, and attempt recording.
    /// </summary>
    internal static class MatSpreadDispatch
    {
        /// <summary>Per-mode vertical reach is derived from the search depth the same way at every
        /// spread-time call site; inspect passes <paramref name="verticalSearch"/> 0 to use defaults.</summary>
        public static bool IsFrontier(IBlockAccessor acc, BlockPos origin, PlantRequirements req, int verticalSearch)
        {
            if (req == null) return true;

            if (req.UsesRhizomeSpread)
            {
                int reach = verticalSearch > 0 ? System.Math.Min(verticalSearch, 3) : RhizomeSpread.DefaultVerticalReach;
                return RhizomeSpread.IsFrontier(acc, origin, req.Species, reach);
            }

            if (req.UsesSurfaceMatSpread)
            {
                int reach = verticalSearch > 0 ? System.Math.Min(verticalSearch, 2) : SurfaceMatSpread.DefaultVerticalReach;
                return SurfaceMatSpread.IsFrontier(acc, origin, req.Species, reach);
            }

            if (req.UsesFernRhizomeSpread)
            {
                return FernRhizomeSpread.IsFrontier(acc, origin, req.Species);
            }

            if (req.UsesBerryColonySpread)
            {
                return BerryColonySpread.IsFrontier(acc, origin, req.Species);
            }

            return true;
        }

        public static bool IsStep(int dx, int dz, PlantRequirements req)
        {
            if (req == null) return true;
            if (req.UsesRhizomeSpread) return RhizomeSpread.IsOrthogonalStep(dx, dz);
            if (req.UsesSurfaceMatSpread) return SurfaceMatSpread.IsMatStep(dx, dz);
            if (req.UsesFernRhizomeSpread) return FernRhizomeSpread.IsOrthogonalStep(dx, dz);
            if (req.UsesBerryColonySpread) return BerryColonySpread.IsStep(dx, dz, req.Species);
            return true;
        }

        public static MatSpreadCollectMode ResolveCollectMode(PlantRequirements req, System.Random rand)
        {
            if (req == null || rand == null) return MatSpreadCollectMode.NotApplicable;
            if (req.UsesRhizomeSpread) return RhizomeSpread.ResolveCollectMode(req, rand);
            if (req.UsesBerryColonySpread) return BerryColonySpread.ResolveCollectMode(req, rand);
            if (req.UsesFernRhizomeSpread) return MatSpreadCollectMode.MatEdge;
            if (req.UsesSurfaceMatSpread) return SurfaceMatSpread.ResolveCollectMode(req, rand);
            return MatSpreadCollectMode.NotApplicable;
        }
    }
}
