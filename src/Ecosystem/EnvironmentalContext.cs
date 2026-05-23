using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public sealed class EnvironmentalContext : IEnvironmentalContext
    {
        public BlockPos Position { get; }
        public float Temperature { get; }
        public float WorldgenRainfall { get; }
        public float ForestDensity { get; }
        public bool InGreenhouse { get; }
        public int GroundFertility { get; }
        public bool GroundSideSolid { get; }
        public int SpaceReplaceable { get; }
        public bool HasClimate { get; }
        public bool TouchesFluid { get; }
        public bool HasShallowWater { get; }

        EnvironmentalContext(
            BlockPos pos,
            float temperature,
            float worldgenRainfall,
            float forestDensity,
            bool inGreenhouse,
            int groundFertility,
            bool groundSideSolid,
            int spaceReplaceable,
            bool hasClimate,
            bool touchesFluid,
            bool hasShallowWater)
        {
            Position = pos;
            Temperature = temperature;
            WorldgenRainfall = worldgenRainfall;
            ForestDensity = forestDensity;
            InGreenhouse = inGreenhouse;
            GroundFertility = groundFertility;
            GroundSideSolid = groundSideSolid;
            SpaceReplaceable = spaceReplaceable;
            HasClimate = hasClimate;
            TouchesFluid = touchesFluid;
            HasShallowWater = hasShallowWater;
        }

        public static EnvironmentalContext Sample(ICoreAPI api, BlockPos plantPos, PlantRequirements requirements = null)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos groundPos = plantPos.DownCopy();
            Block ground = acc.GetBlock(groundPos);
            Block space = acc.GetBlock(plantPos);

            // Seasonal temp from NowValues; rain/forest maps are worldgen-static (WorldGenValues).
            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
            ClimateCondition fallback = worldgen ?? now;

            float temperature = now?.Temperature ?? worldgen?.Temperature ?? 0f;
            float worldgenRainfall = ReadWorldgenRainfall(worldgen, now);
            float forestDensity = worldgen?.ForestDensity ?? now?.ForestDensity ?? 0f;

            bool shallowWater = ComputeWaterRequirement(acc, plantPos, ground, requirements);

            return new EnvironmentalContext(
                plantPos.Copy(),
                temperature,
                worldgenRainfall,
                forestDensity,
                GreenhouseHelper.IsGreenhouse(api, plantPos),
                (int)ground.Fertility,
                ground.SideSolid[BlockFacing.UP.Index],
                space.Replaceable,
                fallback != null,
                BlockFluidHelper.TouchesFluid(acc, plantPos),
                shallowWater);
        }

        static bool ComputeWaterRequirement(IBlockAccessor acc, BlockPos plantPos, Block ground, PlantRequirements requirements)
        {
            if (requirements == null) return false;

            switch (requirements.Habitat)
            {
                case EcologyHabitat.ReedNearWater:
                    int depth = System.Math.Max(4, (requirements.MaxWaterDepth > 0 ? requirements.MaxWaterDepth : 1) + 3);
                    return BlockFluidHelper.HasReedSiltSubstrate(acc, plantPos, depth);
                case EcologyHabitat.WaterSurface:
                    return BlockFluidHelper.HasWaterSurfaceSupport(acc, plantPos);
                default:
                    return false;
            }
        }

        static float ReadWorldgenRainfall(ClimateCondition worldgen, ClimateCondition now)
        {
            if (worldgen != null) return worldgen.WorldgenRainfall;
            if (now != null) return now.WorldgenRainfall;
            return 0f;
        }
    }
}
