using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class SpreadSolveRequest
    {
        public BlockPos Origin;
        public Block SpreadBlock;
        public PlantRequirements Requirements;
        public float MinFitness;
        public bool HarshClimate;
        public int Radius;
        public int VerticalSearch;
        public int MaxSpawns;
        public SpreadCollectPhase Phase;
        public MatSpreadCollectMode MatMode;
        public float SeasonSpreadMult;
        public float SeedFitnessScale;
        public int RandomSeed;
        public bool EmptyFirstTwoPhase;
        public readonly List<SpreadSolveCell> Cells = new List<SpreadSolveCell>();
    }

    internal sealed class SpreadSolveResult
    {
        public BlockPos Origin;
        public Block SpreadBlock;
        public PlantRequirements Requirements;
        public readonly List<SpreadSolveWinner> Winners = new List<SpreadSolveWinner>();
    }

    /// <summary>Captures compact env snapshots on the main thread before worker scoring.</summary>
    internal static class SpreadSolveBatchBuilder
    {
        static readonly HashSet<BlockPos> scratchSeen = new HashSet<BlockPos>();

        internal static bool UsesMatSpread(PlantRequirements requirements) =>
            requirements != null
            && (requirements.UsesRhizomeSpread
                || requirements.UsesSurfaceMatSpread
                || requirements.UsesFernRhizomeSpread
                || requirements.UsesBerryColonySpread);

        internal static bool UsesCrowfootSpread(PlantRequirements requirements) =>
            requirements != null
            && requirements.Habitat == EcologyHabitat.UnderwaterColumn
            && requirements.SpreadMode == SpreadMode.Independent;

        internal static bool CanBackgroundSolve(PlantRequirements requirements)
        {
            if (requirements == null) return false;
            if (UsesMatSpread(requirements)) return true;
            if (UsesCrowfootSpread(requirements)) return true;
            return requirements.Habitat == EcologyHabitat.Terrestrial
                && !requirements.UsesRhizomeSpread
                && !requirements.UsesSurfaceMatSpread
                && !requirements.UsesFernRhizomeSpread
                && !requirements.UsesBerryColonySpread;
        }

        public static bool TryBuildRequest(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand,
            out SpreadSolveRequest request)
        {
            if (UsesMatSpread(requirements))
            {
                return TryBuildMatRequest(
                    api, origin, spreadBlock, requirements, minFitness, harshClimate,
                    radius, verticalSearch, maxSpawns, rand, out request);
            }

            if (UsesCrowfootSpread(requirements))
            {
                return TryBuildCrowfootRequest(
                    api, origin, spreadBlock, requirements, minFitness, harshClimate,
                    radius, verticalSearch, maxSpawns, rand, out request);
            }

            return TryBuildTerrestrialRequest(
                api, origin, spreadBlock, requirements, minFitness, harshClimate,
                radius, verticalSearch, maxSpawns, rand, SpreadCollectPhase.All, out request);
        }

        public static bool TryBuildMatRequest(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand,
            out SpreadSolveRequest request)
        {
            request = null;
            if (api == null || origin == null || spreadBlock == null || requirements == null) return false;
            if (!UsesMatSpread(requirements)) return false;

            int searchRadius = ReproducePlacement.ResolveSpreadSearchRadius(requirements, radius, rand, out MatSpreadCollectMode matMode);
            float seedFitnessScale = matMode == MatSpreadCollectMode.SeedDispersal
                ? System.Math.Max(0.01f, EcosystemConfig.Loaded.RhizomeSeedDispersalFitnessScale)
                : 1f;

            request = new SpreadSolveRequest
            {
                Origin = origin.Copy(),
                SpreadBlock = spreadBlock,
                Requirements = requirements,
                MinFitness = minFitness,
                HarshClimate = harshClimate,
                Radius = searchRadius,
                VerticalSearch = verticalSearch,
                MaxSpawns = maxSpawns,
                Phase = SpreadCollectPhase.All,
                MatMode = matMode,
                SeasonSpreadMult = SeasonEcology.SpreadActivityMultiplier(api, origin, requirements),
                SeedFitnessScale = seedFitnessScale,
                RandomSeed = rand.Next(),
                EmptyFirstTwoPhase = false,
            };

            CollectMatCells(api, request);
            return request.Cells.Count > 0;
        }

        public static bool TryBuildCrowfootRequest(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand,
            out SpreadSolveRequest request)
        {
            request = null;
            if (api == null || origin == null || spreadBlock == null || requirements == null) return false;
            if (!UsesCrowfootSpread(requirements)) return false;

            int searchRadius = ReproducePlacement.ResolveSpreadSearchRadius(requirements, radius, rand, out MatSpreadCollectMode matMode);

            request = new SpreadSolveRequest
            {
                Origin = origin.Copy(),
                SpreadBlock = spreadBlock,
                Requirements = requirements,
                MinFitness = minFitness,
                HarshClimate = harshClimate,
                Radius = searchRadius,
                VerticalSearch = verticalSearch,
                MaxSpawns = maxSpawns,
                Phase = SpreadCollectPhase.All,
                MatMode = matMode,
                SeasonSpreadMult = SeasonEcology.SpreadActivityMultiplier(api, origin, requirements),
                SeedFitnessScale = 1f,
                RandomSeed = rand.Next(),
                EmptyFirstTwoPhase = false,
            };

            CollectCrowfootCells(api, request);
            return request.Cells.Count > 0;
        }

        public static bool TryBuildTerrestrialRequest(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand,
            SpreadCollectPhase phase,
            out SpreadSolveRequest request)
        {
            request = null;
            if (api == null || origin == null || spreadBlock == null || requirements == null) return false;
            if (requirements.Habitat != EcologyHabitat.Terrestrial) return false;
            if (requirements.UsesRhizomeSpread || requirements.UsesSurfaceMatSpread
                || requirements.UsesFernRhizomeSpread || requirements.UsesBerryColonySpread) return false;

            int searchRadius = ReproducePlacement.ResolveSpreadSearchRadius(requirements, radius, rand, out MatSpreadCollectMode matMode);
            float seedFitnessScale = matMode == MatSpreadCollectMode.SeedDispersal
                ? System.Math.Max(0.01f, EcosystemConfig.Loaded.RhizomeSeedDispersalFitnessScale)
                : 1f;

            request = new SpreadSolveRequest
            {
                Origin = origin.Copy(),
                SpreadBlock = spreadBlock,
                Requirements = requirements,
                MinFitness = minFitness,
                HarshClimate = harshClimate,
                Radius = searchRadius,
                VerticalSearch = verticalSearch,
                MaxSpawns = maxSpawns,
                Phase = phase,
                MatMode = matMode,
                SeasonSpreadMult = SeasonEcology.SpreadActivityMultiplier(api, origin, requirements),
                SeedFitnessScale = seedFitnessScale,
                RandomSeed = rand.Next(),
                EmptyFirstTwoPhase = ReproducePlacement.UsesEmptyFirstSpreadCollect(requirements, matMode),
            };

            CollectTerrestrialCells(api, request);
            return request.Cells.Count > 0;
        }

        static void CollectTerrestrialCells(ICoreAPI api, SpreadSolveRequest request)
        {
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos origin = request.Origin;
            PlantRequirements requirements = request.Requirements;
            int radius = request.Radius;
            int verticalSearch = request.VerticalSearch;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            EcologyColumnOccupancy occupancy = cfg.EnableSpreadColumnOccupancyHint
                ? EcosystemSystem.Instance?.SpacingIndex?.ColumnOccupancy
                : null;
            bool useOccupancyHint = occupancy != null && request.Phase != SpreadCollectPhase.All;
            EcologyColumnState ecologyColumns = EcosystemSystem.Instance?.EcologyColumns;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            NicheSampler niche = EcosystemSystem.Instance?.Niche;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int worldX = origin.X + dx;
                    int worldZ = origin.Z + dz;
                    if (useOccupancyHint)
                    {
                        bool columnOccupied = occupancy.IsOccupied(worldX, worldZ);
                        if (request.Phase == SpreadCollectPhase.EmptyOnly && columnOccupied) continue;
                        if (request.Phase == SpreadCollectPhase.DisplacementOnly && !columnOccupied) continue;
                    }

                    if (!SurfacePlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, out BlockPos plantPos, out _, requirements))
                    {
                        continue;
                    }

                    if (requirements.MinSunlight > 0
                        && !TreePlacement.HasEnoughSunlight(acc, plantPos, requirements.MinSunlight))
                    {
                        continue;
                    }

                    if (!scratchSeen.Add(plantPos)) continue;
                    if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) continue;

                    SpreadColumnSnapshot columnSnap = default;
                    bool haveColumnSnap = ecologyColumns != null
                        && cfg.EnableEcologyColumnCache
                        && ecologyColumns.TryGetSpreadSnapshot(api, plantPos, out columnSnap);
                    CellBlockSnapshot snap = haveColumnSnap
                        ? columnSnap.BlockSnap
                        : CellBlockSnapshot.Sample(acc, plantPos);

                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        continue;
                    }

                    BlockPos groundPos = plantPos.DownCopy();
                    if (WildSoilGroundRules.HasActiveMycelium(acc, groundPos)
                        && !MyceliumCoexistence.AllowsMeadowFloraOverMycelium(acc, groundPos, requirements))
                    {
                        continue;
                    }

                    if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) continue;

                    float myceliumMult = 1f;
                    if (cfg.EnableMyceliumNiche)
                    {
                        float baseFitness = 1f;
                        myceliumMult = MyceliumZone.ApplySpreadFitness(api, requirements, plantPos, baseFitness);
                        if (myceliumMult <= 0f) myceliumMult = 1f;
                    }

                    float worldgenRain = haveColumnSnap ? columnSnap.WorldgenRainfall : 0f;
                    bool hasClimate = haveColumnSnap && columnSnap.HasClimate;
                    float localForest = haveColumnSnap ? columnSnap.LocalForestCover : 0f;

                    if (!haveColumnSnap)
                    {
                        EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
                        if (cache == null || !cache.TryGetWorldgenRainfall(acc, plantPos, out worldgenRain, out hasClimate))
                        {
                            ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
                            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
                            worldgenRain = worldgen?.WorldgenRainfall ?? now?.WorldgenRainfall ?? 0f;
                            hasClimate = (worldgen ?? now) != null;
                        }

                        if (flora != null)
                        {
                            localForest = flora.GetLocalForestCover(api, plantPos);
                        }
                    }

                    FloraContext floraContext = FloraContext.Open;
                    if (flora != null && cfg.UseFloraContext)
                    {
                        floraContext = flora.GetContext(api, plantPos);
                    }

                    int nicheMoisture = (int)MoistureLevel.Mesic;
                    int nicheLight = (int)LightLevel.Partial;
                    if (niche != null && cfg.UseNicheContext && requirements.HasNicheProfile)
                    {
                        LocalNiche local = niche.GetNiche(api, plantPos);
                        nicheMoisture = (int)local.Moisture;
                        nicheLight = (int)local.Light;
                    }

                    int groundFertility = (int)snap.Ground.Fertility;
                    SoilKind soilKinds = SoilClassification.Classify(snap.Ground);
                    bool groundSolid = snap.Ground.SideSolid[BlockFacing.UP.Index];
                    if (WildSoilGroundRules.IsFarmland(snap.Ground))
                    {
                        groundFertility = 150;
                        groundSolid = true;
                    }

                    request.Cells.Add(new SpreadSolveCell(
                        plantPos,
                        snap.Space?.Id ?? 0,
                        snap.Ground?.Id ?? 0,
                        PlantVacancyRules.EffectiveSpaceReplaceable(snap.Space),
                        groundFertility,
                        soilKinds,
                        groundSolid,
                        isEmpty,
                        snap.TouchesFluid,
                        hasShallowWater: false,
                        hasClimate,
                        worldgenRain,
                        localForest,
                        floraContext,
                        nicheMoisture,
                        nicheLight,
                        myceliumMult,
                        spacingOk: true));
                }
            }
        }

        static void CollectMatCells(ICoreAPI api, SpreadSolveRequest request)
        {
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos origin = request.Origin;
            PlantRequirements requirements = request.Requirements;
            int radius = request.Radius;
            int verticalSearch = request.VerticalSearch;
            MatSpreadCollectMode matMode = request.MatMode;

            if (matMode == MatSpreadCollectMode.MatEdge
                && !MatSpreadDispatch.IsFrontier(acc, origin, requirements, verticalSearch))
            {
                return;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    if (matMode == MatSpreadCollectMode.MatEdge
                        && !MatSpreadDispatch.IsStep(dx, dz, requirements)) continue;

                    if (!WaterPlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, requirements, out BlockPos plantPos, out _))
                    {
                        continue;
                    }

                    if (!scratchSeen.Add(plantPos)) continue;
                    if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) continue;

                    EcosystemConfig cfg = EcosystemConfig.Loaded;
                    EcologyColumnState ecologyColumns = EcosystemSystem.Instance?.EcologyColumns;
                    SpreadColumnSnapshot columnSnap = default;
                    bool haveColumnSnap = ecologyColumns != null
                        && cfg.EnableEcologyColumnCache
                        && ecologyColumns.TryGetSpreadSnapshot(api, plantPos, out columnSnap);
                    CellBlockSnapshot snap = haveColumnSnap
                        ? columnSnap.BlockSnap
                        : CellBlockSnapshot.Sample(acc, plantPos);

                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        continue;
                    }

                    bool matVacancyOk = isEmpty
                        || SpreadVacancy.CanOccupy(acc, plantPos, requirements, snap.Space, isEmpty);
                    if (!matVacancyOk) continue;

                    if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) continue;

                    AppendEnvCell(api, request, plantPos, in snap, haveColumnSnap, in columnSnap, isEmpty, matVacancyOk);
                }
            }
        }

        static void CollectCrowfootCells(ICoreAPI api, SpreadSolveRequest request)
        {
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos origin = request.Origin;
            PlantRequirements requirements = request.Requirements;
            int radius = request.Radius;
            int verticalSearch = request.VerticalSearch;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    if (!CrowfootPlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, requirements, out BlockPos plantPos, out _))
                    {
                        continue;
                    }

                    if (!scratchSeen.Add(plantPos)) continue;
                    if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) continue;

                    EcosystemConfig cfg = EcosystemConfig.Loaded;
                    EcologyColumnState ecologyColumns = EcosystemSystem.Instance?.EcologyColumns;
                    SpreadColumnSnapshot columnSnap = default;
                    bool haveColumnSnap = ecologyColumns != null
                        && cfg.EnableEcologyColumnCache
                        && ecologyColumns.TryGetSpreadSnapshot(api, plantPos, out columnSnap);
                    CellBlockSnapshot snap = haveColumnSnap
                        ? columnSnap.BlockSnap
                        : CellBlockSnapshot.Sample(acc, plantPos);

                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        continue;
                    }

                    bool crowfootVacancyOk = WaterColumnHelper.IsValidCrowfootSpreadBase(acc, plantPos, requirements);
                    if (!crowfootVacancyOk) continue;

                    if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) continue;

                    if (!WaterColumnHelper.TryMeasureWaterColumn(acc, plantPos, out int waterDepth, out _))
                    {
                        continue;
                    }

                    AppendEnvCell(
                        api,
                        request,
                        plantPos,
                        in snap,
                        haveColumnSnap,
                        in columnSnap,
                        isEmpty,
                        crowfootVacancyOk,
                        waterDepth);
                }
            }
        }

        static void AppendEnvCell(
            ICoreAPI api,
            SpreadSolveRequest request,
            BlockPos plantPos,
            in CellBlockSnapshot snap,
            bool haveColumnSnap,
            in SpreadColumnSnapshot columnSnap,
            bool isEmpty,
            bool matVacancyOk,
            int waterColumnDepth = 0)
        {
            PlantRequirements requirements = request.Requirements;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            FloraContextSampler flora = EcosystemSystem.Instance?.FloraContext;
            NicheSampler niche = EcosystemSystem.Instance?.Niche;

            float worldgenRain = haveColumnSnap ? columnSnap.WorldgenRainfall : 0f;
            bool hasClimate = haveColumnSnap && columnSnap.HasClimate;
            float localForest = haveColumnSnap ? columnSnap.LocalForestCover : 0f;

            if (!haveColumnSnap)
            {
                EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
                IBlockAccessor acc = api.World.BlockAccessor;
                if (cache == null || !cache.TryGetWorldgenRainfall(acc, plantPos, out worldgenRain, out hasClimate))
                {
                    ClimateCondition worldgen = acc.GetClimateAt(plantPos, EnumGetClimateMode.WorldGenValues);
                    ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
                    worldgenRain = worldgen?.WorldgenRainfall ?? now?.WorldgenRainfall ?? 0f;
                    hasClimate = (worldgen ?? now) != null;
                }

                if (flora != null)
                {
                    localForest = flora.GetLocalForestCover(api, plantPos);
                }
            }

            FloraContext floraContext = FloraContext.Open;
            if (flora != null && cfg.UseFloraContext)
            {
                floraContext = flora.GetContext(api, plantPos);
            }

            int nicheMoisture = (int)MoistureLevel.Mesic;
            int nicheLight = (int)LightLevel.Partial;
            if (niche != null && cfg.UseNicheContext && requirements.HasNicheProfile)
            {
                LocalNiche local = niche.GetNiche(api, plantPos);
                nicheMoisture = (int)local.Moisture;
                nicheLight = (int)local.Light;
            }

            int groundFertility = (int)snap.Ground.Fertility;
            SoilKind soilKinds = SoilClassification.Classify(snap.Ground);
            bool groundSolid = snap.Ground.SideSolid[BlockFacing.UP.Index];
            if (WildSoilGroundRules.IsFarmland(snap.Ground))
            {
                groundFertility = 150;
                groundSolid = true;
            }

            request.Cells.Add(new SpreadSolveCell(
                plantPos,
                snap.Space?.Id ?? 0,
                snap.Ground?.Id ?? 0,
                PlantVacancyRules.EffectiveSpaceReplaceable(snap.Space),
                groundFertility,
                soilKinds,
                groundSolid,
                isEmpty,
                snap.TouchesFluid,
                hasShallowWater: requirements.Habitat == EcologyHabitat.ReedNearWater,
                hasClimate,
                worldgenRain,
                localForest,
                floraContext,
                nicheMoisture,
                nicheLight,
                1f,
                spacingOk: true,
                matVacancyOk,
                waterColumnDepth));
        }
    }
}
