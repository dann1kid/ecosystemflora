using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public interface IEnvironmentalContext
    {
        BlockPos Position { get; }
        float Temperature { get; }
        bool InGreenhouse { get; }
        int GroundFertility { get; }
        bool GroundSideSolid { get; }
        int SpaceReplaceable { get; }
        bool HasClimate { get; }
    }
}
