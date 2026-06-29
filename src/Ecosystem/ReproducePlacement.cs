using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal enum SpreadCollectPhase
    {
        All,
        EmptyOnly,
        DisplacementOnly,
    }

    internal static class ReproducePlacement
    {
        static readonly List<SpreadCandidate> scratchCandidates = new List<SpreadCandidate>();
        static readonly HashSet<BlockPos> scratchSeen = new HashSet<BlockPos>();

        readonly struct SpreadCandidate
        {
            public BlockPos Pos { get; }
            public float Fitness { get; }
            public bool Displacing { get; }

            public SpreadCandidate(BlockPos pos, float fitness, bool displacing)
            {
                Pos = pos;
                Fitness = fitness;
                Displacing = displacing;
            }
        }

        public static int TryPlaceSpreadAmongNeighbors(
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
            bool logFailures,
            out string failureReason,
            System.Action<BlockPos, PlantRequirements, bool> onPlaced = null)
        {
            failureReason = null;
            if (maxSpawns <= 0) return 0;

            int searchRadius = ResolveSpreadSearchRadius(requirements, radius, rand, out MatSpreadCollectMode matMode);

            List<SpreadCandidate> candidates = CollectSpreadCandidatesForSpread(
                api, origin, searchRadius, verticalSearch, requirements, minFitness, harshClimate, matMode);

            if (candidates.Count == 0)
            {
                failureReason = "No competitive cells in radius " + searchRadius;
                return 0;
            }

            if (!UsesEmptyFirstSpreadCollect(requirements, matMode)
                && EcosystemConfig.Loaded.PreferSpreadToEmptyCells
                && !TurfColonizerSpread.PrefersOccupiedTurf(requirements?.Species))
            {
                ApplyEmptySpreadPreference(candidates, EcosystemConfig.Loaded.EmptySpreadFitnessMultiplier);
            }

            int placed = 0;
            var remaining = new List<SpreadCandidate>(candidates);

            while (placed < maxSpawns && remaining.Count > 0)
            {
                int index = PickWeightedIndex(remaining, rand);
                SpreadCandidate chosen = remaining[index];
                remaining.RemoveAt(index);

                if (PlaceSpreadBlock(api, chosen.Pos, spreadBlock, requirements, origin, chosen.Displacing))
                {
                    placed++;
                    onPlaced?.Invoke(chosen.Pos, requirements, chosen.Displacing);
                    if (logFailures)
                    {
                        api.Logger.Notification(
                            "[ecosystemflora] {0} {1} at {2} ({3} candidates near {4})",
                            chosen.Displacing ? "Displaced" : "Spread",
                            spreadBlock.Code,
                            chosen.Pos,
                            candidates.Count,
                            origin);
                    }
                }
            }

            if (placed == 0)
            {
                failureReason = "Placement failed after selecting candidate";
            }

            return placed;
        }

        /// <summary>Evaluate spread candidates and enqueue winners without SetBlock (Phase 6.5).</summary>
        public static int TryEnqueueSpreadAmongNeighbors(
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
            PendingSpreadQueue queue,
            bool logFailures,
            out string failureReason)
        {
            failureReason = null;
            if (maxSpawns <= 0 || queue == null) return 0;

            int searchRadius = ResolveSpreadSearchRadius(requirements, radius, rand, out MatSpreadCollectMode matMode);

            List<SpreadCandidate> candidates = CollectSpreadCandidatesForSpread(
                api, origin, searchRadius, verticalSearch, requirements, minFitness, harshClimate, matMode);

            if (candidates.Count == 0)
            {
                failureReason = "No competitive cells in radius " + searchRadius;
                return 0;
            }

            if (!UsesEmptyFirstSpreadCollect(requirements, matMode)
                && EcosystemConfig.Loaded.PreferSpreadToEmptyCells
                && !TurfColonizerSpread.PrefersOccupiedTurf(requirements?.Species))
            {
                ApplyEmptySpreadPreference(candidates, EcosystemConfig.Loaded.EmptySpreadFitnessMultiplier);
            }

            int enqueued = 0;
            var remaining = new List<SpreadCandidate>(candidates);

            while (enqueued < maxSpawns && remaining.Count > 0)
            {
                int index = PickWeightedIndex(remaining, rand);
                SpreadCandidate chosen = remaining[index];
                remaining.RemoveAt(index);

                queue.Enqueue(new PendingSpreadIntent
                {
                    ParentOrigin = origin.Copy(),
                    TargetPos = chosen.Pos.Copy(),
                    SpreadBlock = spreadBlock,
                    Requirements = requirements,
                    Displacing = chosen.Displacing,
                });
                enqueued++;

                if (logFailures)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] Queued {0} {1} at {2} ({3} candidates near {4})",
                        chosen.Displacing ? "displace" : "spread",
                        spreadBlock.Code,
                        chosen.Pos,
                        candidates.Count,
                        origin);
                }
            }

            return enqueued;
        }

        internal static bool TryCommitSpread(ICoreAPI api, PendingSpreadIntent intent, bool logFailures)
        {
            if (api == null || intent?.TargetPos == null || intent.SpreadBlock == null || intent.Requirements == null)
            {
                return false;
            }

            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos targetPos = intent.TargetPos;

            if (!LandClaimGuard.AllowsEcologyChange(api, targetPos)) return false;

            CellBlockSnapshot snap = CellBlockSnapshot.Sample(acc, targetPos);
            if (!SpreadPreflight.PassesPhysicalGate(acc, targetPos, intent.Requirements, in snap, out bool isEmpty))
            {
                return false;
            }

            if (intent.Displacing)
            {
                if (!PlantCodeHelper.IsEcologySpreadParent(snap.Space)
                    || PlantCodeHelper.IsArborealHostBlock(snap.Space))
                {
                    return false;
                }
            }
            else if (!isEmpty && !SpreadVacancy.CanOccupy(acc, targetPos, intent.Requirements, snap.Space, isEmpty))
            {
                return false;
            }

            bool placed = PlaceSpreadBlock(
                api,
                targetPos,
                intent.SpreadBlock,
                intent.Requirements,
                intent.ParentOrigin,
                intent.Displacing);

            if (!placed && logFailures)
            {
                api.Logger.Notification(
                    "[ecosystemflora] Commit failed for {0} at {1}",
                    intent.SpreadBlock.Code,
                    targetPos);
            }

            return placed;
        }

        internal static int ResolveSpreadSearchRadius(
            PlantRequirements requirements,
            int radius,
            System.Random rand,
            out MatSpreadCollectMode matMode)
        {
            matMode = MatSpreadCollectMode.NotApplicable;
            int searchRadius = radius;

            if (requirements != null && requirements.UsesRhizomeSpread)
            {
                matMode = RhizomeSpread.ResolveCollectMode(requirements, rand);
                searchRadius = RhizomeSpread.ResolveSearchRadius(requirements, matMode, radius);
            }
            else if (requirements != null && requirements.UsesFernRhizomeSpread)
            {
                matMode = MatSpreadCollectMode.MatEdge;
                searchRadius = requirements.SpreadRadius > 0 ? requirements.SpreadRadius : 1;
            }
            else if (requirements != null && requirements.UsesBerryColonySpread)
            {
                matMode = BerryColonySpread.ResolveCollectMode(requirements, rand);
                searchRadius = BerryColonySpread.ResolveSearchRadius(requirements, matMode, radius);
            }
            else if (requirements != null && requirements.UsesShoreSedgeMatSpread)
            {
                matMode = ShoreSedgeMatSpread.ResolveCollectMode(requirements, rand);
                searchRadius = ShoreSedgeMatSpread.ResolveSearchRadius(requirements, matMode, radius);
            }
            else if (requirements != null && requirements.UsesSurfaceMatSpread)
            {
                matMode = SurfaceMatSpread.ResolveCollectMode(requirements, rand);
                searchRadius = SurfaceMatSpread.ResolveSearchRadius(requirements, matMode, radius);
            }
            else if (requirements != null && requirements.SpreadRadius > 0)
            {
                searchRadius = requirements.SpreadRadius;
            }

            return searchRadius;
        }

        internal static bool UsesEmptyFirstSpreadCollect(PlantRequirements requirements, MatSpreadCollectMode matMode)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            return cfg.EnableEmptyFirstSpreadCollect
                && requirements?.Habitat == EcologyHabitat.Terrestrial
                && cfg.UseCellDisplacement
                && !TurfColonizerSpread.PrefersOccupiedTurf(requirements?.Species)
                && matMode == MatSpreadCollectMode.NotApplicable;
        }

        static List<SpreadCandidate> CollectSpreadCandidatesForSpread(
            ICoreAPI api,
            BlockPos origin,
            int radius,
            int verticalSearch,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            MatSpreadCollectMode matMode)
        {
            if (!UsesEmptyFirstSpreadCollect(requirements, matMode))
            {
                return CollectSpreadCandidates(
                    api, origin, radius, verticalSearch, requirements, minFitness, harshClimate, matMode,
                    SpreadCollectPhase.All);
            }

            List<SpreadCandidate> emptyCandidates = CollectSpreadCandidates(
                api, origin, radius, verticalSearch, requirements, minFitness, harshClimate, matMode,
                SpreadCollectPhase.EmptyOnly);

            if (emptyCandidates.Count > 0)
            {
                return emptyCandidates;
            }

            return CollectSpreadCandidates(
                api, origin, radius, verticalSearch, requirements, minFitness, harshClimate, matMode,
                SpreadCollectPhase.DisplacementOnly);
        }

        static List<SpreadCandidate> CollectSpreadCandidates(
            ICoreAPI api,
            BlockPos origin,
            int radius,
            int verticalSearch,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            MatSpreadCollectMode matMode,
            SpreadCollectPhase phase)
        {
            scratchCandidates.Clear();
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool diag = cfg.VerboseLogging && cfg.ReproduceDebug;
            EcologyColumnOccupancy occupancy = cfg.EnableSpreadColumnOccupancyHint
                ? EcosystemSystem.Instance?.SpacingIndex?.ColumnOccupancy
                : null;
            bool useOccupancyHint = occupancy != null
                && requirements?.Habitat == EcologyHabitat.Terrestrial
                && phase != SpreadCollectPhase.All;

            if (matMode == MatSpreadCollectMode.MatEdge
                && !MatSpreadDispatch.IsFrontier(acc, origin, requirements, verticalSearch))
            {
                return scratchCandidates;
            }

            float seedFitnessScale = matMode == MatSpreadCollectMode.SeedDispersal
                ? cfg.RhizomeSeedDispersalFitnessScale
                : 1f;
            if (seedFitnessScale <= 0f) seedFitnessScale = 0.01f;

            int dNoSurface = 0, dSunlight = 0, dDupe = 0, dClaim = 0;
            int dPreflight = 0, dOccupied = 0, dFitness = 0, dSpacing = 0, dDisplace = 0, dHint = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    if (matMode == MatSpreadCollectMode.MatEdge
                        && !MatSpreadDispatch.IsStep(dx, dz, requirements)) continue;

                    int worldX = origin.X + dx;
                    int worldZ = origin.Z + dz;
                    if (useOccupancyHint)
                    {
                        bool columnOccupied = occupancy.IsOccupied(worldX, worldZ);
                        if (phase == SpreadCollectPhase.EmptyOnly && columnOccupied)
                        {
                            dHint++;
                            continue;
                        }

                        if (phase == SpreadCollectPhase.DisplacementOnly && !columnOccupied)
                        {
                            dHint++;
                            continue;
                        }
                    }

                    bool foundPos;
                    BlockPos plantPos;
                    if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        int sun = requirements.MinSunlight > 0 ? requirements.MinSunlight : 11;
                        foundPos = TreePlacement.TryFindSaplingPos(
                            acc, origin, dx, dz, verticalSearch, sun, out plantPos, out _);
                    }
                    else if (requirements.Habitat == EcologyHabitat.Ferntree)
                    {
                        int sun = requirements.MinSunlight > 0 ? requirements.MinSunlight : 10;
                        foundPos = TreePlacement.TryFindSaplingPos(
                            acc, origin, dx, dz, verticalSearch, sun, out plantPos, out _);
                    }
                    else if (requirements.Habitat != EcologyHabitat.Terrestrial)
                    {
                        foundPos = WaterPlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, requirements, out plantPos, out _);
                    }
                    else
                    {
                        foundPos = SurfacePlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, out plantPos, out _, requirements);
                    }

                    if (!foundPos) { dNoSurface++; continue; }

                    if (requirements.MinSunlight > 0
                        && !TreePlacement.HasEnoughSunlight(acc, plantPos, requirements.MinSunlight))
                    {
                        dSunlight++;
                        continue;
                    }

                    if (!scratchSeen.Add(plantPos)) { dDupe++; continue; }

                    if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) { dClaim++; continue; }

                    EcologyColumnState ecologyColumns = EcosystemSystem.Instance?.EcologyColumns;
                    CellBlockSnapshot snap;
                    SpreadColumnSnapshot columnSnap = default;
                    bool haveColumnSnap = ecologyColumns != null
                        && cfg.EnableEcologyColumnCache
                        && ecologyColumns.TryGetSpreadSnapshot(api, plantPos, out columnSnap);
                    snap = haveColumnSnap ? columnSnap.BlockSnap : CellBlockSnapshot.Sample(acc, plantPos);

                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        dPreflight++;
                        continue;
                    }

                    float fitness;
                    bool displacing = false;
                    bool canOccupy = isEmpty
                        || SpreadVacancy.CanOccupy(acc, plantPos, requirements, snap.Space, isEmpty);

                    if (phase == SpreadCollectPhase.DisplacementOnly)
                    {
                        if (isEmpty)
                        {
                            dOccupied++;
                            continue;
                        }

                        if (requirements.Habitat != EcologyHabitat.Terrestrial || !cfg.UseCellDisplacement)
                        {
                            dOccupied++;
                            continue;
                        }

                        if (!CellCompetition.CanDisplace(
                                api, requirements, snap.Space, plantPos, harshClimate, in snap,
                                out float challengerScore, out _))
                        {
                            dDisplace++;
                            continue;
                        }

                        fitness = challengerScore;
                        displacing = true;
                        if (fitness < minFitness) { dFitness++; continue; }
                    }
                    else if (canOccupy)
                    {
                        if (phase == SpreadCollectPhase.EmptyOnly && !isEmpty)
                        {
                            dOccupied++;
                            continue;
                        }

                        EnvironmentalContext ctx = haveColumnSnap
                            ? EnvironmentalContext.SampleForSpread(api, plantPos, in columnSnap, requirements)
                            : EnvironmentalContext.SampleForSpread(
                                api, plantPos, in snap, requirements, EcosystemSystem.Instance?.ColumnCache);
                        float climateFitness = CellCompetition.SpreadClimateFitness(
                            api, requirements, plantPos, harshClimate, ctx);
                        if (climateFitness < minFitness) { dFitness++; continue; }
                        fitness = CellCompetition.SpreadAttemptWeight(
                            api, requirements, plantPos, climateFitness);
                        fitness *= seedFitnessScale;
                        if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) { dSpacing++; continue; }
                    }
                    else if (requirements.Habitat == EcologyHabitat.Terrestrial && cfg.UseCellDisplacement)
                    {
                        if (phase == SpreadCollectPhase.EmptyOnly)
                        {
                            dOccupied++;
                            continue;
                        }

                        if (!CellCompetition.CanDisplace(
                            api, requirements, snap.Space, plantPos, harshClimate, in snap,
                            out float challengerScore, out float incumbentScore))
                        {
                            dDisplace++;
                            continue;
                        }

                        fitness = challengerScore;
                        displacing = true;
                        if (fitness < minFitness) { dFitness++; continue; }
                    }
                    else
                    {
                        dOccupied++;
                        continue;
                    }

                    scratchCandidates.Add(new SpreadCandidate(plantPos.Copy(), fitness, displacing));
                }
            }

            if (diag && scratchCandidates.Count == 0)
            {
                api.Logger.Notification(
                    "[ecosystemflora] spread reject {0} at {1} phase={2}: noSurf={3} sun={4} dup={5} claim={6} preflight={7} occup={8} fit={9} space={10} displ={11} hint={12}",
                    requirements.Species ?? "?",
                    origin,
                    phase,
                    dNoSurface, dSunlight, dDupe, dClaim,
                    dPreflight, dOccupied, dFitness, dSpacing, dDisplace, dHint);

                if (requirements.Habitat == EcologyHabitat.Terrestrial && dNoSurface > 0)
                {
                    SurfacePlacement.TryFindPlantPos(
                        acc, origin, 1, 0, verticalSearch, out _, out string surfProbe);
                    api.Logger.Notification(
                        "[ecosystemflora] surface summary {0} dx=1 dz=0 ±{1}: {2}",
                        requirements.Species ?? "?",
                        verticalSearch,
                        surfProbe);

                    SurfacePlacement.LogColumnDyProbe(
                        api, acc, origin, 1, 0, verticalSearch, requirements.Species + " dx+1");
                    SurfacePlacement.LogColumnDyProbe(
                        api, acc, origin, 0, 1, verticalSearch, requirements.Species + " dz+1");
                }
            }

            var result = new List<SpreadCandidate>(scratchCandidates);
            scratchCandidates.Clear();
            scratchSeen.Clear();
            return result;
        }

        /// <summary>Favor empty cells but keep displacement candidates in the weighted pool.</summary>
        static void ApplyEmptySpreadPreference(List<SpreadCandidate> candidates, float emptyMultiplier)
        {
            if (emptyMultiplier <= 1f || candidates.Count == 0) return;

            bool hasEmpty = false;
            bool hasDisplacing = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Displacing) hasDisplacing = true;
                else hasEmpty = true;
            }

            if (!hasEmpty || !hasDisplacing) return;

            for (int i = 0; i < candidates.Count; i++)
            {
                SpreadCandidate c = candidates[i];
                if (!c.Displacing)
                {
                    candidates[i] = new SpreadCandidate(c.Pos, c.Fitness * emptyMultiplier, false);
                }
            }
        }

        static int PickWeightedIndex(List<SpreadCandidate> candidates, System.Random rand)
        {
            if (candidates.Count == 1) return 0;

            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                total += candidates[i].Fitness;
            }

            if (total <= 0f) return rand.Next(candidates.Count);

            float roll = (float)rand.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].Fitness;
                if (roll <= acc) return i;
            }

            return candidates.Count - 1;
        }

        static bool PlaceSpreadBlock(
            ICoreAPI api,
            BlockPos plantPos,
            Block spreadBlock,
            PlantRequirements requirements,
            BlockPos parentOrigin,
            bool displacing)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) return false;

            if (requirements.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                IBlockAccessor acc = api.World.BlockAccessor;

                if (!WaterColumnHelper.TrySnapCrowfootColumnBase(acc, plantPos, out BlockPos columnBase))
                {
                    return false;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, columnBase)) return false;

                int height = CrowfootColumnPlacer.MeasureColumnHeight(acc, parentOrigin);
                if (height < 2) height = 3;

                if (WaterColumnHelper.TryMeasureWaterColumn(acc, columnBase, out int waterDepth, out _))
                {
                    if (waterDepth > 0 && waterDepth < height) height = waterDepth;
                }

                bool preferFlower = spreadBlock.Code?.Path?.Contains("top") == true;
                return CrowfootColumnPlacer.PlaceColumn(api, columnBase, height, preferFlower, api.World.Rand);
            }

            IBlockAccessor accessor = api.World.BlockAccessor;

            if (displacing)
            {
                Block incumbent = accessor.GetBlock(plantPos);
                EcosystemSystem eco = EcosystemSystem.Instance;
                if (eco != null && PlantCodeHelper.IsEcologySpreadParent(incumbent))
                {
                    eco.RemoveEcologyPlant(plantPos, cascadeSymbiosis: true, reason: "displaced");
                    eco.WakeEcologyAround(plantPos);
                }
            }

            if (requirements.Habitat == EcologyHabitat.ReedNearWater)
            {
                spreadBlock = PlantCodeHelper.ResolveReedSpreadBlock(api, plantPos, spreadBlock);
            }

            if (PlantCodeHelper.ResolveEcologySpecies(spreadBlock) == "tallgrass")
            {
                spreadBlock = TallgrassSpreadMaturation.ResolveSpreadBlock(
                    api, plantPos, spreadBlock, requirements, api.World.Rand);
            }

            if (requirements.Habitat == EcologyHabitat.Ferntree)
            {
                int parentSegments = FerntreeStructure.MeasureTrunkSegmentCount(accessor, parentOrigin);
                int segments = System.Math.Max(3, parentSegments - api.World.Rand.Next(2));
                if (!FerntreeStructure.TryPlaceYoung(accessor, plantPos, segments, api.World.Rand))
                {
                    return false;
                }

                accessor.MarkBlockDirty(plantPos);
                return FerntreeStructure.IsTrunkBlock(accessor.GetBlock(plantPos));
            }

            // Trees: never place vanilla saplings from wild spread. We place a tiny log-grown seedling,
            // and the mod's yearly maturation grows it naturally.
            if (IsWildTreeSpreadPlacement(requirements, spreadBlock)
                && TryPlaceWildTreeSeedling(api, accessor, plantPos, requirements, spreadBlock, api.World.Rand))
            {
                accessor.MarkBlockDirty(plantPos);
                return PlantCodeHelper.IsTreeLogGrownBlock(accessor.GetBlock(plantPos));
            }

            if (IsWildTreeSpreadPlacement(requirements, spreadBlock))
            {
                return false;
            }

            ItemStack stack = new ItemStack(spreadBlock);
            accessor.SetBlock(spreadBlock.BlockId, plantPos, stack);

            if (accessor.GetBlock(plantPos).Id != spreadBlock.Id) return false;

            if (EcosystemConfig.Loaded.CloneBerryTraits
                && api.Side == EnumAppSide.Server
                && requirements?.Habitat == EcologyHabitat.Terrestrial
                && PlantCodeHelper.IsWildBerryBushBlock(spreadBlock))
            {
                BerrySpreadTraitCloner.TryCloneFromParent(api, parentOrigin, plantPos);
            }

            accessor.MarkBlockDirty(plantPos);
            return true;
        }

        static bool IsWildTreeSpreadPlacement(PlantRequirements requirements, Block spreadBlock)
        {
            if (requirements?.Habitat == EcologyHabitat.TerrestrialTree) return true;
            return PlantCodeHelper.IsTreeSaplingBlock(spreadBlock)
                || PlantCodeHelper.IsTreeLogGrownBlock(spreadBlock);
        }

        static bool TryPlaceWildTreeSeedling(
            ICoreAPI api,
            IBlockAccessor accessor,
            BlockPos plantPos,
            PlantRequirements requirements,
            Block spreadBlock,
            System.Random rand)
        {
            string wood = requirements?.Species;
            if (string.IsNullOrEmpty(wood))
            {
                wood = PlantCodeHelper.GetTreeWood(spreadBlock?.Code);
            }

            if (string.IsNullOrEmpty(wood)) return false;

            return TreeYoungStructure.TryPlaceSeedling(api, accessor, plantPos, wood, rand);
        }
    }
}
