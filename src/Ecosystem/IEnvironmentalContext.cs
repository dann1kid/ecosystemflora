using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public interface IEnvironmentalContext
    {
        BlockPos Position { get; }
        float Temperature { get; }
        float WorldgenRainfall { get; }
        float ForestDensity { get; }
        bool InGreenhouse { get; }
        int GroundFertility { get; }
        SoilKind GroundSoilKinds { get; }
        bool GroundSideSolid { get; }
        int SpaceReplaceable { get; }
        bool HasClimate { get; }
        bool TouchesFluid { get; }
        bool HasShallowWater { get; }
    }
}
