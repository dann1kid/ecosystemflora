using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Immutable spread inputs for one candidate cell (worker-safe, no BlockAccessor).</summary>
    internal readonly struct SpreadSolveCell
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;
        public readonly int SpaceBlockId;
        public readonly int GroundBlockId;
        public readonly int SpaceReplaceable;
        public readonly int GroundFertility;
        public readonly SoilKind GroundSoilKinds;
        public readonly bool GroundSideSolid;
        public readonly bool IsEmpty;
        public readonly bool TouchesFluid;
        public readonly bool HasShallowWater;
        public readonly bool HasClimate;
        public readonly float WorldgenRainfall;
        public readonly float LocalForestCover;
        public readonly FloraContext FloraContext;
        public readonly int NicheMoisture;
        public readonly int NicheLight;
        public readonly float MyceliumFitnessMult;
        public readonly float TrafficFitnessMult;
        public readonly bool SpacingOk;
        public readonly bool MatVacancyOk;
        /// <summary>Contiguous water layers at crowfoot column base (0 when N/A).</summary>
        public readonly int WaterColumnDepth;

        public SpreadSolveCell(
            BlockPos pos,
            int spaceBlockId,
            int groundBlockId,
            int spaceReplaceable,
            int groundFertility,
            SoilKind groundSoilKinds,
            bool groundSideSolid,
            bool isEmpty,
            bool touchesFluid,
            bool hasShallowWater,
            bool hasClimate,
            float worldgenRainfall,
            float localForestCover,
            FloraContext floraContext,
            int nicheMoisture,
            int nicheLight,
            float myceliumFitnessMult,
            bool spacingOk,
            bool matVacancyOk = true,
            int waterColumnDepth = 0,
            float trafficFitnessMult = 1f)
        {
            X = pos.X;
            Y = pos.Y;
            Z = pos.Z;
            SpaceBlockId = spaceBlockId;
            GroundBlockId = groundBlockId;
            SpaceReplaceable = spaceReplaceable;
            GroundFertility = groundFertility;
            GroundSoilKinds = groundSoilKinds;
            GroundSideSolid = groundSideSolid;
            IsEmpty = isEmpty;
            TouchesFluid = touchesFluid;
            HasShallowWater = hasShallowWater;
            HasClimate = hasClimate;
            WorldgenRainfall = worldgenRainfall;
            LocalForestCover = localForestCover;
            FloraContext = floraContext;
            NicheMoisture = nicheMoisture;
            NicheLight = nicheLight;
            MyceliumFitnessMult = myceliumFitnessMult <= 0f ? 1f : myceliumFitnessMult;
            TrafficFitnessMult = trafficFitnessMult <= 0f ? 0f : trafficFitnessMult;
            SpacingOk = spacingOk;
            MatVacancyOk = matVacancyOk;
            WaterColumnDepth = waterColumnDepth;
        }

        public BlockPos ToPos() => new BlockPos(X, Y, Z);
    }
}
