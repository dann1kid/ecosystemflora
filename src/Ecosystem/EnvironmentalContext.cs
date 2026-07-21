using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public sealed class EnvironmentalContext : IEnvironmentalContext
    {
        public BlockPos Position { get; }
        public float Temperature { get; }
        public float WorldgenRainfall { get; }
        public float LocalForestCover { get; }
        public bool InGreenhouse { get; }
        public int GroundFertility { get; }
        public SoilKind GroundSoilKinds { get; }
        public bool GroundSideSolid { get; }
        public int SpaceReplaceable { get; }
        public bool HasClimate { get; }
        public bool TouchesFluid { get; }
        public bool HasShallowWater { get; }

        EnvironmentalContext(
            BlockPos pos,
            float temperature,
            float worldgenRainfall,
            float localForestCover,
            bool inGreenhouse,
            int groundFertility,
            SoilKind groundSoilKinds,
            bool groundSideSolid,
            int spaceReplaceable,
            bool hasClimate,
            bool touchesFluid,
            bool hasShallowWater)
        {
            Position = pos;
            Temperature = temperature;
            WorldgenRainfall = worldgenRainfall;
            LocalForestCover = localForestCover;
            InGreenhouse = inGreenhouse;
            GroundFertility = groundFertility;
            GroundSoilKinds = groundSoilKinds;
            GroundSideSolid = groundSideSolid;
            SpaceReplaceable = spaceReplaceable;
            HasClimate = hasClimate;
            TouchesFluid = touchesFluid;
            HasShallowWater = hasShallowWater;
        }

        /// <summary>Full context for stress / survival (temp, greenhouse, climate).</summary>
        public static EnvironmentalContext Sample(
            ICoreAPI api,
            BlockPos plantPos,
            PlantRequirements requirements = null)
        {
            return SampleForSurvival(api, plantPos, requirements);
        }

        /// <summary>Spread/displace fitness — no seasonal temp or greenhouse lookup.</summary>
        public static EnvironmentalContext SampleForSpread(
            ICoreAPI api,
            BlockPos plantPos,
            PlantRequirements requirements = null)
        {
            return SampleForSpread(api, plantPos, requirements, EcosystemSystem.Instance?.ColumnCache);
        }

        /// <summary>Stress / survival — spread fields plus seasonal temperature and greenhouse.</summary>
        public static EnvironmentalContext SampleForSurvival(
            ICoreAPI api,
            BlockPos plantPos,
            PlantRequirements requirements = null)
        {
            return SampleForSurvival(api, plantPos, requirements, EcosystemSystem.Instance?.ColumnCache);
        }

        /// <summary>
        /// Yearly tree niche lifespan check: worldgen temperature (not season), rainfall, and local forest
        /// cover with the trunk's own crown footprint excluded.
        /// </summary>
        public static EnvironmentalContext SampleForTreeNicheLifespan(
            ICoreAPI api,
            BlockPos trunkBase,
            PlantRequirements requirements,
            string wood)
        {
            EnvironmentalContext spread = SampleForSpread(
                api,
                trunkBase,
                requirements,
                EcosystemSystem.Instance?.ColumnCache);

            float temperature = ReadWorldgenTemperature(api, trunkBase);

            float localForest = spread.LocalForestCover;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            if (flora != null && !string.IsNullOrEmpty(wood))
            {
                TreeStructureMetrics metrics = TreeStructureProbe.Measure(
                    api.World.BlockAccessor,
                    trunkBase,
                    wood);
                localForest = flora.GetLocalForestCoverExcludingSelf(
                    api,
                    trunkBase,
                    metrics.CrownRadius);
            }

            return new EnvironmentalContext(
                spread.Position,
                temperature,
                spread.WorldgenRainfall,
                localForest,
                GreenhouseHelper.IsGreenhouse(api, trunkBase),
                spread.GroundFertility,
                spread.GroundSoilKinds,
                spread.GroundSideSolid,
                spread.SpaceReplaceable,
                spread.HasClimate,
                spread.TouchesFluid,
                spread.HasShallowWater);
        }

        static float ReadWorldgenTemperature(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return 0f;
            ClimateCondition worldgen = api.World.BlockAccessor.GetClimateAt(
                plantPos,
                EnumGetClimateMode.WorldGenValues);
            if (worldgen != null) return worldgen.Temperature;

            ClimateCondition now = api.World.BlockAccessor.GetClimateAt(
                plantPos,
                EnumGetClimateMode.NowValues);
            return now?.Temperature ?? 0f;
        }

        internal static EnvironmentalContext SampleForSpread(
            ICoreAPI api,
            BlockPos plantPos,
            PlantRequirements requirements,
            EnvironmentalColumnCache cache)
        {
            CellBlockSnapshot snap = CellBlockSnapshot.Sample(api.World.BlockAccessor, plantPos);
            return SampleForSpread(api, plantPos, in snap, requirements, cache);
        }

        internal static EnvironmentalContext SampleForSpread(
            ICoreAPI api,
            BlockPos plantPos,
            in CellBlockSnapshot snap,
            PlantRequirements requirements,
            EnvironmentalColumnCache cache)
        {
            EcologyColumnState ecology = EcosystemSystem.Instance?.EcologyColumns;
            if (ecology != null
                && EcosystemConfig.Loaded.EnableEcologyColumnCache
                && ecology.TryGetSpreadSnapshot(api, plantPos, out SpreadColumnSnapshot columnSnap))
            {
                return SampleForSpread(api, plantPos, in columnSnap, requirements);
            }

            IBlockAccessor acc = api.World.BlockAccessor;

            float worldgenRainfall;
            bool hasClimate;
            if (cache != null && cache.TryGetWorldgenRainfall(acc, plantPos, out worldgenRainfall, out hasClimate))
            {
                // cached
            }
            else
            {
                ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
                ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
                ClimateCondition fallback = worldgen ?? now;
                worldgenRainfall = ReadWorldgenRainfall(worldgen, now);
                hasClimate = fallback != null;
            }

            float localForestCover = 0f;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            if (flora != null)
            {
                localForestCover = flora.GetLocalForestCover(api, plantPos);
            }

            return BuildSpreadContext(api, plantPos, in snap, requirements, worldgenRainfall, hasClimate, localForestCover);
        }

        internal static EnvironmentalContext SampleForSpread(
            ICoreAPI api,
            BlockPos plantPos,
            in SpreadColumnSnapshot columnSnap,
            PlantRequirements requirements)
        {
            return BuildSpreadContext(
                api,
                plantPos,
                in columnSnap.BlockSnap,
                requirements,
                columnSnap.WorldgenRainfall,
                columnSnap.HasClimate,
                columnSnap.LocalForestCover);
        }

        internal static EnvironmentalContext FromSpreadSolveCell(in SpreadSolveCell cell)
        {
            return new EnvironmentalContext(
                cell.ToPos(),
                0f,
                cell.WorldgenRainfall,
                cell.LocalForestCover,
                false,
                cell.GroundFertility,
                cell.GroundSoilKinds,
                cell.GroundSideSolid,
                cell.SpaceReplaceable,
                cell.HasClimate,
                cell.TouchesFluid,
                cell.HasShallowWater || cell.WaterColumnDepth > 0);
        }

        static EnvironmentalContext BuildSpreadContext(
            ICoreAPI api,
            BlockPos plantPos,
            in CellBlockSnapshot snap,
            PlantRequirements requirements,
            float worldgenRainfall,
            bool hasClimate,
            float localForestCover)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            bool shallowWater = ComputeWaterRequirement(acc, plantPos, snap.Ground, requirements);

            int groundFertility = (int)snap.Ground.Fertility;
            SoilKind soilKinds = SoilClassification.Classify(snap.Ground);
            bool groundSolid = snap.Ground.SideSolid[BlockFacing.UP.Index];

            bool isFarmland = WildSoilGroundRules.IsFarmland(snap.Ground);
            if (isFarmland)
            {
                groundFertility = 150;
                groundSolid = true;
            }

            return new EnvironmentalContext(
                plantPos,
                0f,
                worldgenRainfall,
                localForestCover,
                false,
                groundFertility,
                soilKinds,
                groundSolid,
                PlantVacancyRules.EffectiveSpaceReplaceable(snap.Space),
                hasClimate,
                snap.TouchesFluid,
                shallowWater);
        }

        internal static EnvironmentalContext SampleForSurvival(
            ICoreAPI api,
            BlockPos plantPos,
            PlantRequirements requirements,
            EnvironmentalColumnCache cache)
        {
            EnvironmentalContext spread = SampleForSpread(api, plantPos, requirements, cache);
            IBlockAccessor acc = api.World.BlockAccessor;

            float temperature;
            if (cache == null || !cache.TryGetNowTemperature(acc, plantPos, out temperature))
            {
                ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
                temperature = now?.Temperature ?? 0f;
            }

            return new EnvironmentalContext(
                spread.Position,
                temperature,
                spread.WorldgenRainfall,
                spread.LocalForestCover,
                GreenhouseHelper.IsGreenhouse(api, plantPos),
                spread.GroundFertility,
                spread.GroundSoilKinds,
                spread.GroundSideSolid,
                spread.SpaceReplaceable,
                spread.HasClimate,
                spread.TouchesFluid,
                spread.HasShallowWater);
        }

        static bool ComputeWaterRequirement(IBlockAccessor acc, BlockPos plantPos, Block ground, PlantRequirements requirements)
        {
            if (requirements == null) return false;

            switch (requirements.Habitat)
            {
                case EcologyHabitat.ReedNearWater:
                    return ReedColumnHelper.IsValidReedPlantSite(acc, plantPos, requirements, out _);
                case EcologyHabitat.WaterSurface:
                    return BlockFluidHelper.HasWaterSurfaceSupport(acc, plantPos);
                case EcologyHabitat.UnderwaterColumn:
                    if (!WaterColumnHelper.TryMeasureUnderwaterColumnDepth(acc, plantPos, out int waterDepth, out bool hasSubstrate))
                    {
                        return false;
                    }

                    int minDepth = requirements.MinWaterDepth > 0 ? requirements.MinWaterDepth : 2;
                    return hasSubstrate && waterDepth >= minDepth && waterDepth <= requirements.MaxWaterDepth;
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
