using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public sealed class EnvironmentalContext : IEnvironmentalContext
    {
        public BlockPos Position { get; }
        public float Temperature { get; }
        public bool InGreenhouse { get; }
        public int GroundFertility { get; }
        public bool GroundSideSolid { get; }
        public int SpaceReplaceable { get; }
        public bool HasClimate { get; }
        public bool TouchesFluid { get; }

        EnvironmentalContext(
            BlockPos pos,
            float temperature,
            bool inGreenhouse,
            int groundFertility,
            bool groundSideSolid,
            int spaceReplaceable,
            bool hasClimate,
            bool touchesFluid)
        {
            Position = pos;
            Temperature = temperature;
            InGreenhouse = inGreenhouse;
            GroundFertility = groundFertility;
            GroundSideSolid = groundSideSolid;
            SpaceReplaceable = spaceReplaceable;
            HasClimate = hasClimate;
            TouchesFluid = touchesFluid;
        }

        public static EnvironmentalContext Sample(ICoreAPI api, BlockPos plantPos)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos groundPos = plantPos.DownCopy();
            Block ground = acc.GetBlock(groundPos);
            Block space = acc.GetBlock(plantPos);

            ClimateCondition conds = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);

            return new EnvironmentalContext(
                plantPos.Copy(),
                conds?.Temperature ?? 0f,
                GreenhouseHelper.IsGreenhouse(api, plantPos),
                (int)ground.Fertility,
                ground.SideSolid[BlockFacing.UP.Index],
                space.Replaceable,
                conds != null,
                BlockFluidHelper.TouchesFluid(acc, plantPos));
        }
    }
}
