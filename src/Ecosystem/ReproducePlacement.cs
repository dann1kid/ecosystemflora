using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
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

            if (requirements != null && requirements.SpreadRadius > 0)
            {
                radius = requirements.SpreadRadius;
            }

            List<SpreadCandidate> candidates = CollectSpreadCandidates(
                api, origin, radius, verticalSearch, requirements, minFitness, harshClimate);

            if (candidates.Count == 0)
            {
                failureReason = "No competitive cells in radius " + radius;
                return 0;
            }

            if (EcosystemConfig.Loaded.PreferSpreadToEmptyCells)
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

        static List<SpreadCandidate> CollectSpreadCandidates(
            ICoreAPI api,
            BlockPos origin,
            int radius,
            int verticalSearch,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate)
        {
            scratchCandidates.Clear();
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool diag = cfg.VerboseLogging && cfg.ReproduceDebug;

            if (requirements.UsesRhizomeSpread
                && !RhizomeSpread.IsFrontier(
                    acc,
                    origin,
                    requirements.Species,
                    verticalSearch > 0 ? System.Math.Min(verticalSearch, 3) : RhizomeSpread.DefaultVerticalReach))
            {
                return scratchCandidates;
            }

            int dNoSurface = 0, dSunlight = 0, dDupe = 0, dClaim = 0;
            int dPreflight = 0, dOccupied = 0, dFitness = 0, dSpacing = 0, dDisplace = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    if (requirements.UsesRhizomeSpread && !RhizomeSpread.IsOrthogonalStep(dx, dz)) continue;

                    bool foundPos;
                    BlockPos plantPos;
                    if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        int sun = requirements.MinSunlight > 0 ? requirements.MinSunlight : 11;
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
                            acc, origin, dx, dz, verticalSearch, out plantPos, out _);
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

                    CellBlockSnapshot snap = CellBlockSnapshot.Sample(acc, plantPos);
                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        dPreflight++;
                        continue;
                    }

                    float fitness;
                    bool displacing = false;
                    bool canOccupy = isEmpty
                        || SpreadVacancy.CanOccupy(acc, plantPos, requirements, snap.Space, isEmpty);

                    if (canOccupy)
                    {
                        EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
                        EnvironmentalContext ctx = EnvironmentalContext.SampleForSpread(
                            api, plantPos, in snap, requirements, cache);
                        fitness = CellCompetition.SpreadScoreFromContext(
                            api, requirements, plantPos, harshClimate, ctx);
                        if (fitness < minFitness) { dFitness++; continue; }
                        if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) { dSpacing++; continue; }
                    }
                    else if (requirements.Habitat == EcologyHabitat.Terrestrial && cfg.UseCellDisplacement)
                    {
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
                    "[ecosystemflora] spread reject {0} at {1}: noSurf={2} sun={3} dup={4} claim={5} preflight={6} occup={7} fit={8} space={9} displ={10}",
                    requirements.Species ?? "?",
                    origin,
                    dNoSurface, dSunlight, dDupe, dClaim,
                    dPreflight, dOccupied, dFitness, dSpacing, dDisplace);

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
                }
            }

            if (requirements.Habitat == EcologyHabitat.ReedNearWater)
            {
                spreadBlock = PlantCodeHelper.ResolveReedSpreadBlock(api, plantPos, spreadBlock);
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
    }
}
